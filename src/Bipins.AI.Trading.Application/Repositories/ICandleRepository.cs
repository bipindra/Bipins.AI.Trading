using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Repositories;

public interface ICandleRepository
{
    Task<Candle?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(Candle candle, CancellationToken cancellationToken = default);
    Task<List<Candle>> GetCandlesAsync(Symbol symbol, Timeframe timeframe, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Candle?> GetLatestCandleAsync(Symbol symbol, Timeframe timeframe, CancellationToken cancellationToken = default);
}
