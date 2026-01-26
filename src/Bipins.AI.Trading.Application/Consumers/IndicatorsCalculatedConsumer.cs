using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Strategies;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using MassTransit;

namespace Bipins.AI.Trading.Application.Consumers;

public class IndicatorsCalculatedConsumer : IConsumer<IndicatorsCalculated>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly StrategyExecutor _strategyExecutor;
    private readonly IndicatorService _indicatorService;
    private readonly IPortfolioService _portfolioService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IndicatorsCalculatedConsumer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    public IndicatorsCalculatedConsumer(
        IStrategyRepository strategyRepository,
        ICandleRepository candleRepository,
        StrategyExecutor strategyExecutor,
        IndicatorService indicatorService,
        IPortfolioService portfolioService,
        IPublishEndpoint publishEndpoint,
        ILogger<IndicatorsCalculatedConsumer> logger,
        ILoggerFactory loggerFactory)
    {
        _strategyRepository = strategyRepository;
        _candleRepository = candleRepository;
        _strategyExecutor = strategyExecutor;
        _indicatorService = indicatorService;
        _portfolioService = portfolioService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    
    public async Task Consume(ConsumeContext<IndicatorsCalculated> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming IndicatorsCalculated: {Symbol} {Timeframe} at {Timestamp}",
            message.Symbol, message.Timeframe, message.CandleTimestamp);
        
        var symbol = new Symbol(message.Symbol);
        var timeframe = new Timeframe(message.Timeframe);
        
        // Get enabled strategies for this timeframe
        var strategies = await _strategyRepository.GetEnabledAsync(context.CancellationToken);
        var relevantStrategies = strategies.Where(s => s.Timeframe.Value == timeframe.Value).ToList();
        
        if (!relevantStrategies.Any())
        {
            _logger.LogDebug("No enabled strategies for timeframe {Timeframe}", timeframe.Value);
            return;
        }
        
        // Get candles for strategy evaluation
        var from = message.CandleTimestamp.AddDays(-7);
        var to = message.CandleTimestamp;
        var candles = await _candleRepository.GetCandlesAsync(symbol, timeframe, from, to, context.CancellationToken);
        
        if (candles.Count < 14)
        {
            _logger.LogDebug("Insufficient candles for strategy evaluation");
            return;
        }
        
        // Convert indicator dictionary to IndicatorResult objects
        var indicators = ConvertToIndicatorResults(message.Indicators, message.CandleTimestamp);
        
        // Get portfolio
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(context.CancellationToken);
        
        // Evaluate each strategy
        foreach (var strategy in relevantStrategies)
        {
            try
            {
                var configurableStrategy = new ConfigurableStrategy(
                    strategy,
                    _strategyExecutor,
                    _loggerFactory.CreateLogger<ConfigurableStrategy>());
                
                var decision = await configurableStrategy.EvaluateAsync(
                    symbol, timeframe, candles, indicators, portfolio, context.CancellationToken);
                
                if (decision != null)
                {
                    // Publish TradeProposed
                    await _publishEndpoint.Publish(new TradeProposed(
                        decision.Id,
                        decision.Symbol.Value,
                        decision.Timeframe.Value,
                        decision.CandleTimestamp,
                        decision.Action.ToString(),
                        decision.QuantityPercent,
                        decision.Quantity?.Value,
                        decision.SuggestedStopLoss?.Amount,
                        decision.SuggestedTakeProfit?.Amount,
                        decision.Confidence,
                        decision.Rationale,
                        decision.Features,
                        message.CorrelationId), context.CancellationToken);
                    
                    _logger.LogInformation("Strategy {StrategyName} generated {Action} decision for {Symbol}",
                        strategy.Name, decision.Action, symbol.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating strategy {StrategyName}", strategy.Name);
            }
        }
    }
    
    private Dictionary<string, IndicatorResult> ConvertToIndicatorResults(
        Dictionary<string, Dictionary<string, object>> indicatorsDict,
        DateTime timestamp)
    {
        var results = new Dictionary<string, IndicatorResult>();
        
        foreach (var kvp in indicatorsDict)
        {
            IndicatorResult? result = kvp.Key switch
            {
                "MACD" => new MACDResult
                {
                    MACD = Convert.ToDecimal(kvp.Value.GetValueOrDefault("MACD", 0m)),
                    Signal = Convert.ToDecimal(kvp.Value.GetValueOrDefault("Signal", 0m)),
                    Histogram = Convert.ToDecimal(kvp.Value.GetValueOrDefault("Histogram", 0m)),
                    Timestamp = timestamp
                },
                "RSI" => new RSIResult
                {
                    Value = Convert.ToDecimal(kvp.Value.GetValueOrDefault("Value", 0m)),
                    Timestamp = timestamp
                },
                "Stochastic" => new StochasticResult
                {
                    PercentK = Convert.ToDecimal(kvp.Value.GetValueOrDefault("PercentK", 0m)),
                    PercentD = Convert.ToDecimal(kvp.Value.GetValueOrDefault("PercentD", 0m)),
                    Timestamp = timestamp
                },
                _ => null
            };
            
            if (result != null)
            {
                results[kvp.Key] = result;
            }
        }
        
        return results;
    }
}
