using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class FillRepository : IFillRepository
{
    private readonly TradingDbContext _context;
    
    public FillRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(Fill fill, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(fill);
        _context.Fills.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Fill>> GetFillsAsync(string? orderId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Fills.AsQueryable();
        
        if (!string.IsNullOrEmpty(orderId))
            query = query.Where(f => f.OrderId == orderId);
        
        if (from.HasValue)
            query = query.Where(f => f.FilledAt >= from.Value);
        
        if (to.HasValue)
            query = query.Where(f => f.FilledAt <= to.Value);
        
        var entities = await query.OrderByDescending(f => f.FilledAt).ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }
    
    private static Fill MapToDomain(FillEntity entity) => new()
    {
        Id = entity.FillId,
        OrderId = entity.OrderId,
        ClientOrderId = entity.ClientOrderId,
        Symbol = new Domain.ValueObjects.Symbol(entity.Symbol),
        Side = entity.GetSide(),
        Quantity = new Domain.ValueObjects.Quantity(entity.Quantity),
        Price = new Domain.ValueObjects.Money(entity.Price),
        Commission = new Domain.ValueObjects.Money(entity.Commission),
        FilledAt = entity.FilledAt
    };
    
    private static FillEntity MapToEntity(Fill fill) => new()
    {
        FillId = fill.Id,
        OrderId = fill.OrderId,
        ClientOrderId = fill.ClientOrderId,
        Symbol = fill.Symbol.Value,
        Side = fill.Side.ToString(),
        Quantity = fill.Quantity.Value,
        Price = fill.Price.Amount,
        Commission = fill.Commission.Amount,
        FilledAt = fill.FilledAt
    };
}
