using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Repositories;

public interface ITickRepository
{
    Task AddAsync(Tick tick, CancellationToken cancellationToken = default);
    Task AddBatchAsync(List<Tick> ticks, CancellationToken cancellationToken = default);
    Task<List<Tick>> GetTicksAsync(Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Tick?> GetLatestTickAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
