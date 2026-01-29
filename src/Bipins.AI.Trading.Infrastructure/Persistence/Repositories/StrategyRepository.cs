using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

using Bipins.AI.Trading.Application.Repositories;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Repositories;

public class StrategyRepository : IStrategyRepository
{
    private readonly TradingDbContext _context;
    
    public StrategyRepository(TradingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Strategy?> GetByIdAsync(string strategyId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Strategies
            .FirstOrDefaultAsync(s => s.StrategyId == strategyId, cancellationToken);
        
        return entity == null ? null : MapToDomain(entity);
    }
    
    public async Task<List<Strategy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Strategies
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
        
        return entities.Select(MapToDomain).ToList();
    }
    
    public async Task<List<Strategy>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Strategies
            .Where(s => s.Enabled)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
        
        return entities.Select(MapToDomain).ToList();
    }
    
    public async Task AddAsync(Strategy strategy, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(strategy);
        _context.Strategies.Add(entity);
        
        // Add alerts
        foreach (var alert in strategy.Alerts)
        {
            _context.IndicatorAlerts.Add(MapAlertToEntity(alert, strategy.Id));
        }
        
        // Add conditions
        foreach (var condition in strategy.Conditions)
        {
            _context.AlertConditions.Add(MapConditionToEntity(condition, strategy.Id));
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(Strategy strategy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Strategies
            .FirstOrDefaultAsync(s => s.StrategyId == strategy.Id, cancellationToken);
        
        if (entity == null) return;
        
        // Update strategy
        entity.Name = strategy.Name;
        entity.Description = strategy.Description;
        entity.Enabled = strategy.Enabled;
        entity.Timeframe = strategy.Timeframe.Value;
        entity.FinalAction = strategy.FinalAction?.ToString();
        entity.UpdatedAt = DateTime.UtcNow;
        
        // Remove old alerts and conditions
        var oldAlerts = await _context.IndicatorAlerts
            .Where(a => a.StrategyId == strategy.Id)
            .ToListAsync(cancellationToken);
        var oldConditions = await _context.AlertConditions
            .Where(c => c.StrategyId == strategy.Id)
            .ToListAsync(cancellationToken);
        
        _context.IndicatorAlerts.RemoveRange(oldAlerts);
        _context.AlertConditions.RemoveRange(oldConditions);
        
        // Add new alerts and conditions
        foreach (var alert in strategy.Alerts)
        {
            _context.IndicatorAlerts.Add(MapAlertToEntity(alert, strategy.Id));
        }
        
        foreach (var condition in strategy.Conditions)
        {
            _context.AlertConditions.Add(MapConditionToEntity(condition, strategy.Id));
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task DeleteAsync(string strategyId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Strategies
            .FirstOrDefaultAsync(s => s.StrategyId == strategyId, cancellationToken);
        
        if (entity == null) return;
        
        var alerts = await _context.IndicatorAlerts
            .Where(a => a.StrategyId == strategyId)
            .ToListAsync(cancellationToken);
        var conditions = await _context.AlertConditions
            .Where(c => c.StrategyId == strategyId)
            .ToListAsync(cancellationToken);
        
        _context.IndicatorAlerts.RemoveRange(alerts);
        _context.AlertConditions.RemoveRange(conditions);
        _context.Strategies.Remove(entity);
        
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    private Strategy MapToDomain(StrategyEntity entity)
    {
        var alerts = _context.IndicatorAlerts
            .Where(a => a.StrategyId == entity.StrategyId)
            .OrderBy(a => a.Order)
            .ToList();
        
        var conditions = _context.AlertConditions
            .Where(c => c.StrategyId == entity.StrategyId)
            .OrderBy(c => c.Order)
            .ToList();
        
        var strategy = new Strategy
        {
            Id = entity.StrategyId,
            Name = entity.Name,
            Description = entity.Description,
            Enabled = entity.Enabled,
            Timeframe = new Timeframe(entity.Timeframe),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Alerts = alerts.Select(MapAlertToDomain).ToList(),
            Conditions = conditions.Select(MapConditionToDomain).ToList()
        };
        
        // Map FinalAction
        if (!string.IsNullOrEmpty(entity.FinalAction) && Enum.TryParse<TradeAction>(entity.FinalAction, out var finalAction))
        {
            strategy.FinalAction = finalAction;
        }
        
        return strategy;
    }
    
    private IndicatorAlert MapAlertToDomain(IndicatorAlertEntity entity)
    {
        return new IndicatorAlert
        {
            Id = entity.AlertId,
            StrategyId = entity.StrategyId,
            IndicatorType = Enum.Parse<IndicatorType>(entity.IndicatorType),
            ConditionType = Enum.Parse<AlertConditionType>(entity.ConditionType),
            Threshold = entity.Threshold,
            TargetField = entity.TargetField,
            Timeframe = new Timeframe(entity.Timeframe),
            Action = Enum.Parse<TradeAction>(entity.Action),
            Order = entity.Order
        };
    }
    
    private AlertCondition MapConditionToDomain(AlertConditionEntity entity)
    {
        return new AlertCondition
        {
            Id = entity.ConditionId,
            StrategyId = entity.StrategyId,
            LeftAlertId = entity.LeftAlertId,
            Operator = Enum.Parse<ConditionOperator>(entity.Operator),
            RightAlertId = entity.RightAlertId,
            LeftConditionId = entity.LeftConditionId,
            RightConditionId = entity.RightConditionId,
            Action = Enum.Parse<TradeAction>(entity.Action),
            Order = entity.Order
        };
    }
    
    private StrategyEntity MapToEntity(Strategy strategy)
    {
        return new StrategyEntity
        {
            StrategyId = strategy.Id,
            Name = strategy.Name,
            Description = strategy.Description,
            Enabled = strategy.Enabled,
            Timeframe = strategy.Timeframe.Value,
            FinalAction = strategy.FinalAction?.ToString(),
            CreatedAt = strategy.CreatedAt,
            UpdatedAt = strategy.UpdatedAt ?? DateTime.UtcNow
        };
    }
    
    private IndicatorAlertEntity MapAlertToEntity(IndicatorAlert alert, string strategyId)
    {
        return new IndicatorAlertEntity
        {
            AlertId = alert.Id,
            StrategyId = strategyId,
            IndicatorType = alert.IndicatorType.ToString(),
            ConditionType = alert.ConditionType.ToString(),
            Threshold = alert.Threshold,
            TargetField = alert.TargetField,
            Timeframe = alert.Timeframe.Value,
            Action = alert.Action.ToString(),
            Order = alert.Order
        };
    }
    
    private AlertConditionEntity MapConditionToEntity(AlertCondition condition, string strategyId)
    {
        return new AlertConditionEntity
        {
            ConditionId = condition.Id,
            StrategyId = strategyId,
            LeftAlertId = condition.LeftAlertId,
            Operator = condition.Operator.ToString(),
            RightAlertId = condition.RightAlertId,
            LeftConditionId = condition.LeftConditionId,
            RightConditionId = condition.RightConditionId,
            Action = condition.Action.ToString(),
            Order = condition.Order
        };
    }
}
