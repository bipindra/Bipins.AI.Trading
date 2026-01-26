using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Ports;

public interface IDecisionEngine
{
    Task<TradeDecision> MakeDecisionAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, decimal> features,
        Portfolio portfolio,
        CancellationToken cancellationToken = default);
}
