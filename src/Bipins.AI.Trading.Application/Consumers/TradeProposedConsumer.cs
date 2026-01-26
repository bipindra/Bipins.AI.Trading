using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Application.Consumers;

public class TradeProposedConsumer : IConsumer<TradeProposed>
{
    private readonly IRiskManager _riskManager;
    private readonly IPortfolioService _portfolioService;
    private readonly TradingOptions _tradingOptions;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TradeProposedConsumer> _logger;
    
    public TradeProposedConsumer(
        IRiskManager riskManager,
        IPortfolioService portfolioService,
        IOptions<TradingOptions> tradingOptions,
        IPublishEndpoint publishEndpoint,
        ILogger<TradeProposedConsumer> logger)
    {
        _riskManager = riskManager;
        _portfolioService = portfolioService;
        _tradingOptions = tradingOptions.Value;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<TradeProposed> context)
    {
        var message = context.Message;
        
        // Check if trading is enabled
        if (!_tradingOptions.Enabled)
        {
            _logger.LogInformation("Trading disabled, rejecting trade proposal {DecisionId}", message.DecisionId);
            await _publishEndpoint.Publish(new TradeRejected(
                message.DecisionId,
                message.Symbol,
                "Trading is disabled",
                message.CorrelationId), context.CancellationToken);
            return;
        }
        
        // Get current portfolio
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(context.CancellationToken);
        
        // Convert to domain decision
        var decision = new TradeDecision
        {
            Id = message.DecisionId,
            Symbol = new Symbol(message.Symbol),
            Timeframe = new Timeframe(message.Timeframe),
            CandleTimestamp = message.CandleTimestamp,
            Action = Enum.Parse<TradeAction>(message.Action),
            QuantityPercent = message.QuantityPercent,
            Quantity = message.Quantity.HasValue ? new Quantity(message.Quantity.Value) : null,
            SuggestedStopLoss = message.SuggestedStopLoss.HasValue ? new Money(message.SuggestedStopLoss.Value) : null,
            SuggestedTakeProfit = message.SuggestedTakeProfit.HasValue ? new Money(message.SuggestedTakeProfit.Value) : null,
            Confidence = message.Confidence,
            Rationale = message.Rationale,
            Features = message.Features
        };
        
        // Risk check
        var riskResult = await _riskManager.CheckTradeAsync(decision, portfolio, context.CancellationToken);
        
        if (!riskResult.IsAllowed)
        {
            _logger.LogWarning("Trade rejected by risk manager: {Reason}", riskResult.Reason);
            await _publishEndpoint.Publish(new TradeRejected(
                message.DecisionId,
                message.Symbol,
                riskResult.Reason ?? "Risk check failed",
                message.CorrelationId), context.CancellationToken);
            return;
        }
        
        // If Ask mode, publish ActionRequired
        if (_tradingOptions.Mode == TradingMode.Ask)
        {
            _logger.LogInformation("Ask mode: publishing ActionRequired for decision {DecisionId}", message.DecisionId);
            var actionRequired = new ActionRequired(
                message.DecisionId,
                message.Symbol,
                message.Action,
                message.Quantity,
                message.Confidence,
                message.Rationale,
                new Dictionary<string, object>
                {
                    ["RiskCheck"] = "Passed",
                    ["Warnings"] = riskResult.Warnings
                },
                DateTime.UtcNow,
                message.CorrelationId);
            
            await _publishEndpoint.Publish(actionRequired, context.CancellationToken);
            
            // ActionRequired message published - portal will consume it
        }
        else
        {
            // Auto mode: approve automatically
            _logger.LogInformation("Auto mode: auto-approving trade {DecisionId}", message.DecisionId);
            await _publishEndpoint.Publish(new TradeApproved(
                message.DecisionId,
                message.Symbol,
                message.Action,
                message.Quantity,
                message.SuggestedStopLoss,
                "System",
                DateTime.UtcNow,
                message.CorrelationId), context.CancellationToken);
        }
    }
}
