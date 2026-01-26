using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly TradingDbContext _context;
    
    public OrderRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order?> GetByClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Orders
            .FirstOrDefaultAsync(o => o.ClientOrderId == clientOrderId, cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var existing = await GetByClientOrderIdAsync(order.ClientOrderId, cancellationToken);
        if (existing != null) return; // Idempotent
        
        var entity = MapToEntity(order);
        _context.Orders.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Orders
            .FirstOrDefaultAsync(o => o.ClientOrderId == order.ClientOrderId, cancellationToken);
        
        if (entity == null) return;
        
        entity.Status = order.Status.ToString();
        entity.SubmittedAt = order.SubmittedAt;
        entity.FilledAt = order.FilledAt;
        entity.RejectionReason = order.RejectionReason;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();
        
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value.ToString());
        
        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);
        
        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);
        
        var entities = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }
    
    private static Order MapToDomain(OrderEntity entity) => new()
    {
        Id = entity.OrderId,
        ClientOrderId = entity.ClientOrderId,
        Symbol = new Domain.ValueObjects.Symbol(entity.Symbol),
        Side = entity.GetSide(),
        Type = entity.GetType(),
        Quantity = new Domain.ValueObjects.Quantity(entity.Quantity),
        LimitPrice = entity.LimitPrice.HasValue ? new Domain.ValueObjects.Money(entity.LimitPrice.Value) : null,
        StopPrice = entity.StopPrice.HasValue ? new Domain.ValueObjects.Money(entity.StopPrice.Value) : null,
        Status = entity.GetStatus(),
        CreatedAt = entity.CreatedAt,
        SubmittedAt = entity.SubmittedAt,
        FilledAt = entity.FilledAt,
        RejectionReason = entity.RejectionReason
    };
    
    private static OrderEntity MapToEntity(Order order) => new()
    {
        OrderId = order.Id,
        ClientOrderId = order.ClientOrderId,
        Symbol = order.Symbol.Value,
        Side = order.Side.ToString(),
        Type = order.Type.ToString(),
        Quantity = order.Quantity.Value,
        LimitPrice = order.LimitPrice?.Amount,
        StopPrice = order.StopPrice?.Amount,
        Status = order.Status.ToString(),
        CreatedAt = order.CreatedAt,
        SubmittedAt = order.SubmittedAt,
        FilledAt = order.FilledAt,
        RejectionReason = order.RejectionReason
    };
}
