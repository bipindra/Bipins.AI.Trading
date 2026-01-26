using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Strategies;

public class ConfigurableStrategy : IStrategy
{
    private readonly Strategy _strategy;
    private readonly StrategyExecutor _executor;
    private readonly ILogger<ConfigurableStrategy> _logger;
    
    public ConfigurableStrategy(
        Strategy strategy,
        StrategyExecutor executor,
        ILogger<ConfigurableStrategy> logger)
    {
        _strategy = strategy;
        _executor = executor;
        _logger = logger;
    }
    
    public string Name => _strategy.Name;
    
    public async Task<TradeDecision?> EvaluateAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, IndicatorResult> indicators,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        if (_strategy.Timeframe.Value != timeframe.Value)
        {
            return null; // Timeframe mismatch
        }
        
        return await _executor.ExecuteAsync(_strategy, symbol, candles, indicators, portfolio);
    }
    
    public List<string> GetRequiredIndicators()
    {
        return _strategy.Alerts
            .Select(a => a.IndicatorType.ToString())
            .Distinct()
            .ToList();
    }
    
    public bool CanEvaluate(List<Candle> candles, Dictionary<string, IndicatorResult> indicators)
    {
        var requiredIndicators = GetRequiredIndicators();
        return requiredIndicators.All(ind => indicators.ContainsKey(ind)) && candles.Count >= 14;
    }
}
