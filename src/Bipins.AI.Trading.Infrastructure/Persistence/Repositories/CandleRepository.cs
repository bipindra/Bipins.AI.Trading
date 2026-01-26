using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class CandleRepository : ICandleRepository
{
    private readonly TradingDbContext _context;
    
    public CandleRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Candle?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var parts = key.Split('_');
        if (parts.Length != 3) return null;
        
        var symbol = parts[0];
        var timeframe = parts[1];
        if (!DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var timestamp))
            return null;
        
        var entity = await _context.Candles
            .FirstOrDefaultAsync(c => c.Symbol == symbol && c.Timeframe == timeframe && c.Timestamp == timestamp, cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    public async Task AddAsync(Candle candle, CancellationToken cancellationToken = default)
    {
        var key = candle.GetIdempotencyKey();
        var existing = await GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existing != null) return; // Idempotent
        
        var entity = MapToEntity(candle);
        _context.Candles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Candle>> GetCandlesAsync(Symbol symbol, Timeframe timeframe, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Candles
            .Where(c => c.Symbol == symbol.Value && c.Timeframe == timeframe.Value && c.Timestamp >= from && c.Timestamp <= to)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(cancellationToken);
        
        return entities.Select(MapToDomain).ToList();
    }
    
    public async Task<Candle?> GetLatestCandleAsync(Symbol symbol, Timeframe timeframe, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Candles
            .Where(c => c.Symbol == symbol.Value && c.Timeframe == timeframe.Value)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    private static Candle MapToDomain(CandleEntity entity) => new()
    {
        Symbol = new Symbol(entity.Symbol),
        Timeframe = new Timeframe(entity.Timeframe),
        Timestamp = entity.Timestamp,
        Open = entity.Open,
        High = entity.High,
        Low = entity.Low,
        Close = entity.Close,
        Volume = entity.Volume
    };
    
    private static CandleEntity MapToEntity(Candle candle) => new()
    {
        Symbol = candle.Symbol.Value,
        Timeframe = candle.Timeframe.Value,
        Timestamp = candle.Timestamp,
        Open = candle.Open,
        High = candle.High,
        Low = candle.Low,
        Close = candle.Close,
        Volume = candle.Volume
    };
}
