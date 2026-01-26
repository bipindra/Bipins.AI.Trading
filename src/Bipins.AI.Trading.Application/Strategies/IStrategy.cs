using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Strategies;

public interface IStrategy
{
    string Name { get; }
    Task<TradeDecision?> EvaluateAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, IndicatorResult> indicators,
        Portfolio portfolio,
        CancellationToken cancellationToken = default);
    
    List<string> GetRequiredIndicators();
    bool CanEvaluate(List<Candle> candles, Dictionary<string, IndicatorResult> indicators);
}
