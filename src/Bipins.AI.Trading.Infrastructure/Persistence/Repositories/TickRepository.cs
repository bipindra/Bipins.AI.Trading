using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class TickRepository : ITickRepository
{
    private readonly TradingDbContext _context;
    
    public TickRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(Tick tick, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(tick);
        _context.Ticks.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task AddBatchAsync(List<Tick> ticks, CancellationToken cancellationToken = default)
    {
        var entities = ticks.Select(MapToEntity).ToList();
        _context.Ticks.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Tick>> GetTicksAsync(Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Ticks
            .Where(t => t.Symbol == symbol.Value && t.Timestamp >= from && t.Timestamp <= to)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);
        
        return entities.Select(MapToDomain).ToList();
    }
    
    public async Task<Tick?> GetLatestTickAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Ticks
            .Where(t => t.Symbol == symbol.Value)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    private static Tick MapToDomain(TickEntity entity) => new()
    {
        Symbol = new Symbol(entity.Symbol),
        Timestamp = entity.Timestamp,
        Price = entity.Price,
        Volume = entity.Volume,
        Bid = entity.Bid,
        Ask = entity.Ask,
        BidSize = entity.BidSize,
        AskSize = entity.AskSize
    };
    
    private static TickEntity MapToEntity(Tick tick) => new()
    {
        Symbol = tick.Symbol.Value,
        Timestamp = tick.Timestamp,
        Price = tick.Price,
        Volume = tick.Volume,
        Bid = tick.Bid,
        Ask = tick.Ask,
        BidSize = tick.BidSize,
        AskSize = tick.AskSize
    };
}
