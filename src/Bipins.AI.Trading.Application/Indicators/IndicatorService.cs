using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Indicators;

public class IndicatorService
{
    private readonly IndicatorRegistry _registry;
    private readonly ILogger<IndicatorService> _logger;
    
    public IndicatorService(IndicatorRegistry registry, ILogger<IndicatorService> logger)
    {
        _registry = registry;
        _logger = logger;
    }
    
    public TResult Calculate<TResult>(
        string indicatorName,
        List<Candle> candles,
        Dictionary<string, object>? config = null) where TResult : IndicatorResult
    {
        var calculator = _registry.Get<TResult>(indicatorName);
        if (calculator == null)
        {
            throw new InvalidOperationException($"Indicator '{indicatorName}' not found");
        }
        
        if (!calculator.CanCalculate(candles))
        {
            throw new InvalidOperationException($"Insufficient candles for indicator '{indicatorName}'");
        }
        
        try
        {
            return calculator.Calculate(candles, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating indicator {Indicator}", indicatorName);
            throw;
        }
    }
    
    public Dictionary<string, IndicatorResult> CalculateAll(
        List<string> indicatorNames,
        List<Candle> candles,
        Dictionary<string, Dictionary<string, object>>? configs = null)
    {
        var results = new Dictionary<string, IndicatorResult>();
        
        foreach (var indicatorName in indicatorNames)
        {
            try
            {
                var config = configs?.GetValueOrDefault(indicatorName);
                IndicatorResult? result = indicatorName switch
                {
                    "MACD" => Calculate<MACDResult>(indicatorName, candles, config),
                    "RSI" => Calculate<RSIResult>(indicatorName, candles, config),
                    "Stochastic" => Calculate<StochasticResult>(indicatorName, candles, config),
                    _ => null
                };
                
                if (result != null)
                {
                    results[indicatorName] = result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate indicator {Indicator}", indicatorName);
            }
        }
        
        return results;
    }
    
    public bool CanCalculate(string indicatorName, List<Candle> candles)
    {
        return indicatorName switch
        {
            "MACD" => _registry.Get<MACDResult>(indicatorName)?.CanCalculate(candles) ?? false,
            "RSI" => _registry.Get<RSIResult>(indicatorName)?.CanCalculate(candles) ?? false,
            "Stochastic" => _registry.Get<StochasticResult>(indicatorName)?.CanCalculate(candles) ?? false,
            _ => false
        };
    }
}
