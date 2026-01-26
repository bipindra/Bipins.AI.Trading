using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Repositories;

public interface IFillRepository
{
    Task AddAsync(Fill fill, CancellationToken cancellationToken = default);
    Task<List<Fill>> GetFillsAsync(string? orderId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
