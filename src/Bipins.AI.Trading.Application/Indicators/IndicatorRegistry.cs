namespace Bipins.AI.Trading.Application.Indicators;

public class IndicatorRegistry
{
    private readonly Dictionary<string, object> _calculators = new();
    
    public void Register<TResult>(IIndicatorCalculator<TResult> calculator) where TResult : IndicatorResult
    {
        _calculators[calculator.Name] = calculator;
    }
    
    public IIndicatorCalculator<TResult>? Get<TResult>(string name) where TResult : IndicatorResult
    {
        if (_calculators.TryGetValue(name, out var calculator))
        {
            return calculator as IIndicatorCalculator<TResult>;
        }
        return null;
    }
    
    public List<string> GetAvailableIndicators()
    {
        return _calculators.Keys.ToList();
    }
    
    public bool IsRegistered(string name)
    {
        return _calculators.ContainsKey(name);
    }
}
