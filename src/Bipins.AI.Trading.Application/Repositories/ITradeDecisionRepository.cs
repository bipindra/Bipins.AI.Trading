using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Repositories;

public interface ITradeDecisionRepository
{
    Task<TradeDecision?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(TradeDecision decision, CancellationToken cancellationToken = default);
    Task<List<TradeDecision>> GetDecisionsAsync(string? symbol = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
