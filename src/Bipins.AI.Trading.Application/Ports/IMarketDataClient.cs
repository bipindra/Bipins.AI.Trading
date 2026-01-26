using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Ports;

public interface IMarketDataClient
{
    Task<List<Candle>> GetHistoricalCandlesAsync(
        Symbol symbol,
        Timeframe timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
    
    Task<List<Candle>> PollLatestCandlesAsync(
        List<Symbol> symbols,
        Timeframe timeframe,
        DateTime? since = null,
        CancellationToken cancellationToken = default);
    
    Task<decimal> GetCurrentPriceAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
