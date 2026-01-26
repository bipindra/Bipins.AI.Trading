using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Strategies;

public class StrategyExecutor
{
    private readonly AlertEvaluator _alertEvaluator;
    private readonly ConditionCombiner _conditionCombiner;
    private readonly IndicatorHistoryService? _historyService;
    private readonly ILogger<StrategyExecutor> _logger;
    
    public StrategyExecutor(
        AlertEvaluator alertEvaluator,
        ConditionCombiner conditionCombiner,
        ILogger<StrategyExecutor> logger,
        IndicatorHistoryService? historyService = null)
    {
        _alertEvaluator = alertEvaluator;
        _conditionCombiner = conditionCombiner;
        _historyService = historyService;
        _logger = logger;
    }
    
    public async Task<TradeDecision?> ExecuteAsync(
        Strategy strategy,
        Symbol symbol,
        List<Candle> candles,
        Dictionary<string, IndicatorResult> indicators,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        if (!strategy.Enabled)
        {
            return null;
        }
        
        if (candles.Count < 14) // Minimum for most indicators
        {
            return null;
        }
        
        var latestCandle = candles.Last();
        var alerts = strategy.Alerts.ToDictionary(a => a.Id, a => a);
        
        // Save indicators to history for crossing detection
        if (_historyService != null)
        {
            foreach (var indicator in indicators)
            {
                await _historyService.SaveIndicatorAsync(
                    symbol.Value,
                    strategy.Timeframe.Value,
                    indicator.Key,
                    indicator.Value,
                    cancellationToken);
            }
        }
        
        // Evaluate individual alerts
        var matchedAlerts = new List<IndicatorAlert>();
        var evaluator = new AlertEvaluator(_historyService, symbol.Value, strategy.Timeframe.Value);
        foreach (var alert in strategy.Alerts)
        {
            if (await evaluator.EvaluateAsync(alert, indicators, cancellationToken))
            {
                matchedAlerts.Add(alert);
                _logger.LogDebug("Alert {AlertId} matched for strategy {StrategyName}", alert.Id, strategy.Name);
            }
        }
        
        // Evaluate conditions
        TradeAction? action = null;
        if (strategy.Conditions.Any())
        {
            var conditionMatched = await _conditionCombiner.EvaluateAllAsync(
                strategy.Conditions, alerts, indicators, symbol.Value, strategy.Timeframe.Value, cancellationToken);
            if (conditionMatched)
            {
                // Use action from first matched condition
                action = strategy.Conditions.First().Action;
            }
        }
        else if (matchedAlerts.Any())
        {
            // If no conditions, use action from first matched alert
            action = matchedAlerts.First().Action;
        }
        
        if (!action.HasValue || action.Value == TradeAction.Hold)
        {
            return null;
        }
        
        // Create trade decision
        var decision = new TradeDecision
        {
            Symbol = symbol,
            Timeframe = strategy.Timeframe,
            CandleTimestamp = latestCandle.Timestamp,
            Action = action.Value,
            Confidence = CalculateConfidence(matchedAlerts.Count, strategy.Alerts.Count),
            Rationale = BuildRationale(strategy, matchedAlerts),
            Features = indicators.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        };
        
        return decision;
    }
    
    private decimal CalculateConfidence(int matchedAlerts, int totalAlerts)
    {
        if (totalAlerts == 0) return 0.5m;
        return Math.Min(1.0m, (decimal)matchedAlerts / totalAlerts);
    }
    
    private string BuildRationale(Strategy strategy, List<IndicatorAlert> matchedAlerts)
    {
        var parts = new List<string> { $"Strategy: {strategy.Name}" };
        
        foreach (var alert in matchedAlerts)
        {
            parts.Add($"{alert.IndicatorType} {alert.ConditionType} matched");
        }
        
        return string.Join("; ", parts);
    }
}
