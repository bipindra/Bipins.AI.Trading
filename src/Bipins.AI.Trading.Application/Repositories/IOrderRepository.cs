using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
