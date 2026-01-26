using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Repositories;

public interface IStrategyRepository
{
    Task<Strategy?> GetByIdAsync(string strategyId, CancellationToken cancellationToken = default);
    Task<List<Strategy>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Strategy>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Strategy strategy, CancellationToken cancellationToken = default);
    Task UpdateAsync(Strategy strategy, CancellationToken cancellationToken = default);
    Task DeleteAsync(string strategyId, CancellationToken cancellationToken = default);
}
