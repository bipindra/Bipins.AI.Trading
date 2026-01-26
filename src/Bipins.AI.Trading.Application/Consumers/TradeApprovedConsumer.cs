using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using MassTransit;

namespace Bipins.AI.Trading.Application.Consumers;

public class TradeApprovedConsumer : IConsumer<TradeApproved>
{
    private readonly IBrokerClient _brokerClient;
    private readonly IMarketDataClient _marketDataClient;
    private readonly IOrderExecutionPolicy _executionPolicy;
    private readonly IPortfolioService _portfolioService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TradeApprovedConsumer> _logger;
    
    public TradeApprovedConsumer(
        IBrokerClient brokerClient,
        IMarketDataClient marketDataClient,
        IOrderExecutionPolicy executionPolicy,
        IPortfolioService portfolioService,
        IPublishEndpoint publishEndpoint,
        ILogger<TradeApprovedConsumer> logger)
    {
        _brokerClient = brokerClient;
        _marketDataClient = marketDataClient;
        _executionPolicy = executionPolicy;
        _portfolioService = portfolioService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<TradeApproved> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming TradeApproved: {DecisionId} for {Symbol}",
            message.DecisionId, message.Symbol);
        
        // Get portfolio to calculate order quantity
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(context.CancellationToken);
        
        // Calculate quantity
        decimal quantity = 0;
        if (message.Quantity.HasValue)
        {
            quantity = message.Quantity.Value;
        }
        else
        {
            // Calculate from portfolio percentage (would need decision stored)
            // For now, use a default
            var symbol = new Symbol(message.Symbol);
            var currentPrice = await _marketDataClient.GetCurrentPriceAsync(symbol, context.CancellationToken);
            var positionPercent = await _portfolioService.GetPositionPercentAsync(symbol, context.CancellationToken);
            var targetPercent = 5.0m; // Default 5%
            var targetValue = portfolio.Equity.Amount * (targetPercent / 100);
            quantity = targetValue / currentPrice;
        }
        
        if (quantity <= 0)
        {
            _logger.LogWarning("Invalid quantity calculated for {Symbol}, skipping order", message.Symbol);
            return;
        }
        
        // Create order request
        var orderRequest = new OrderRequest
        {
            Symbol = new Symbol(message.Symbol),
            Side = message.Action == "Buy" ? OrderSide.Buy : OrderSide.Sell,
            Type = OrderType.Market,
            Quantity = new Quantity(Math.Abs(quantity))
        };
        
        // Check execution policy
        var canExecute = await _executionPolicy.CanExecuteAsync(
            new Order
            {
                Symbol = orderRequest.Symbol,
                Side = orderRequest.Side,
                Type = orderRequest.Type,
                Quantity = orderRequest.Quantity
            }, context.CancellationToken);
        
        if (!canExecute)
        {
            _logger.LogWarning("Order execution policy rejected order for {Symbol}", message.Symbol);
            return;
        }
        
        // Submit order
        try
        {
            var order = await _brokerClient.SubmitOrderAsync(orderRequest, context.CancellationToken);
            
            _logger.LogInformation("Order submitted: {OrderId} for {Symbol}", order.Id, message.Symbol);
            
            // Publish OrderSubmitted
            await _publishEndpoint.Publish(new OrderSubmitted(
                order.Id,
                order.ClientOrderId,
                order.Symbol.Value,
                order.Side.ToString(),
                order.Type.ToString(),
                order.Quantity.Value,
                order.LimitPrice?.Amount,
                order.SubmittedAt ?? order.CreatedAt,
                message.CorrelationId), context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order for {Symbol}", message.Symbol);
        }
    }
}
