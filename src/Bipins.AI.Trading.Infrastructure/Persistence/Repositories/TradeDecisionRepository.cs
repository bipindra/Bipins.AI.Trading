using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class TradeDecisionRepository : ITradeDecisionRepository
{
    private readonly TradingDbContext _context;
    
    public TradeDecisionRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task<TradeDecision?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var parts = key.Split('_');
        if (parts.Length != 3) return null;
        
        var symbol = parts[0];
        var timeframe = parts[1];
        if (!DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var timestamp))
            return null;
        
        var entity = await _context.TradeDecisions
            .FirstOrDefaultAsync(d => d.Symbol == symbol && d.Timeframe == timeframe && d.CandleTimestamp == timestamp, cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    public async Task AddAsync(TradeDecision decision, CancellationToken cancellationToken = default)
    {
        var key = decision.GetIdempotencyKey();
        var existing = await GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existing != null) return; // Idempotent
        
        var entity = MapToEntity(decision);
        _context.TradeDecisions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<TradeDecision>> GetDecisionsAsync(string? symbol = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TradeDecisions.AsQueryable();
        
        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(d => d.Symbol == symbol);
        
        if (from.HasValue)
            query = query.Where(d => d.DecisionTimestamp >= from.Value);
        
        if (to.HasValue)
            query = query.Where(d => d.DecisionTimestamp <= to.Value);
        
        var entities = await query.OrderByDescending(d => d.DecisionTimestamp).ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }
    
    private static TradeDecision MapToDomain(TradeDecisionEntity entity) => new()
    {
        Id = entity.DecisionId,
        Symbol = new Symbol(entity.Symbol),
        Timeframe = new Timeframe(entity.Timeframe),
        CandleTimestamp = entity.CandleTimestamp,
        DecisionTimestamp = entity.DecisionTimestamp,
        Action = entity.GetAction(),
        QuantityPercent = entity.QuantityPercent,
        Quantity = entity.Quantity.HasValue ? new Quantity(entity.Quantity.Value) : null,
        SuggestedStopLoss = entity.SuggestedStopLoss.HasValue ? new Money(entity.SuggestedStopLoss.Value) : null,
        SuggestedTakeProfit = entity.SuggestedTakeProfit.HasValue ? new Money(entity.SuggestedTakeProfit.Value) : null,
        Confidence = entity.Confidence,
        Rationale = entity.Rationale,
        Features = entity.GetFeatures()
    };
    
    private static TradeDecisionEntity MapToEntity(TradeDecision decision)
    {
        var entity = new TradeDecisionEntity
        {
            DecisionId = decision.Id,
            Symbol = decision.Symbol.Value,
            Timeframe = decision.Timeframe.Value,
            CandleTimestamp = decision.CandleTimestamp,
            DecisionTimestamp = decision.DecisionTimestamp,
            Action = decision.Action.ToString(),
            QuantityPercent = decision.QuantityPercent,
            Quantity = decision.Quantity?.Value,
            SuggestedStopLoss = decision.SuggestedStopLoss?.Amount,
            SuggestedTakeProfit = decision.SuggestedTakeProfit?.Amount,
            Confidence = decision.Confidence,
            Rationale = decision.Rationale
        };
        entity.SetFeatures(decision.Features);
        return entity;
    }
}
