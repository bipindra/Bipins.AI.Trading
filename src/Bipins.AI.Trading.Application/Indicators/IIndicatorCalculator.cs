using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Indicators;

public interface IIndicatorCalculator<TResult> where TResult : IndicatorResult
{
    string Name { get; }
    TResult Calculate(List<Candle> candles, Dictionary<string, object>? config = null);
    bool CanCalculate(List<Candle> candles);
    List<string> GetRequiredConfigKeys();
}
