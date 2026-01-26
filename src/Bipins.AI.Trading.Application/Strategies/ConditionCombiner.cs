using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Strategies;

public class ConditionCombiner
{
    private readonly AlertEvaluator _alertEvaluator;
    
    public ConditionCombiner(AlertEvaluator alertEvaluator)
    {
        _alertEvaluator = alertEvaluator;
    }
    
    public async Task<bool> EvaluateAsync(AlertCondition condition, Dictionary<string, IndicatorAlert> alerts, Dictionary<string, IndicatorResult> indicators, string? symbol = null, string? timeframe = null, CancellationToken cancellationToken = default)
    {
        bool leftResult = false;
        bool rightResult = false;
        
        var evaluator = new AlertEvaluator(null, symbol, timeframe);
        
        // Evaluate left side
        if (!string.IsNullOrEmpty(condition.LeftAlertId) && alerts.TryGetValue(condition.LeftAlertId, out var leftAlert))
        {
            leftResult = await evaluator.EvaluateAsync(leftAlert, indicators, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(condition.LeftConditionId))
        {
            // Nested condition - would need to look up and evaluate recursively
            // For now, simplified - would need condition lookup
            leftResult = false;
        }
        
        // Evaluate right side
        if (!string.IsNullOrEmpty(condition.RightAlertId) && alerts.TryGetValue(condition.RightAlertId, out var rightAlert))
        {
            rightResult = await evaluator.EvaluateAsync(rightAlert, indicators, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(condition.RightConditionId))
        {
            // Nested condition
            rightResult = false;
        }
        
        // Combine results
        return condition.Operator switch
        {
            ConditionOperator.And => leftResult && rightResult,
            ConditionOperator.Or => leftResult || rightResult,
            _ => false
        };
    }
    
    public bool Evaluate(AlertCondition condition, Dictionary<string, IndicatorAlert> alerts, Dictionary<string, IndicatorResult> indicators)
    {
        return EvaluateAsync(condition, alerts, indicators).GetAwaiter().GetResult();
    }
    
    public async Task<bool> EvaluateAllAsync(List<AlertCondition> conditions, Dictionary<string, IndicatorAlert> alerts, Dictionary<string, IndicatorResult> indicators, string? symbol = null, string? timeframe = null, CancellationToken cancellationToken = default)
    {
        if (!conditions.Any()) return false;
        
        // Evaluate all conditions and combine with OR (any condition can trigger)
        var tasks = conditions.Select(c => EvaluateAsync(c, alerts, indicators, symbol, timeframe, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.Any(r => r);
    }
    
    public bool EvaluateAll(List<AlertCondition> conditions, Dictionary<string, IndicatorAlert> alerts, Dictionary<string, IndicatorResult> indicators)
    {
        return EvaluateAllAsync(conditions, alerts, indicators).GetAwaiter().GetResult();
    }
}
