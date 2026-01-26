using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Strategies;

public class AlertEvaluator
{
    private readonly IndicatorHistoryService? _historyService;
    private readonly string? _symbol;
    private readonly string? _timeframe;
    
    public AlertEvaluator(IndicatorHistoryService? historyService = null, string? symbol = null, string? timeframe = null)
    {
        _historyService = historyService;
        _symbol = symbol;
        _timeframe = timeframe;
    }
    
    public async Task<bool> EvaluateAsync(IndicatorAlert alert, Dictionary<string, IndicatorResult> indicators, CancellationToken cancellationToken = default)
    {
        if (!indicators.TryGetValue(alert.IndicatorType.ToString(), out var indicator))
        {
            return false;
        }
        
        return alert.IndicatorType switch
        {
            IndicatorType.MACD => await EvaluateMACDAlertAsync(alert, indicator as MACDResult, cancellationToken),
            IndicatorType.RSI => EvaluateRSIAlert(alert, indicator as RSIResult),
            IndicatorType.Stochastic => EvaluateStochasticAlert(alert, indicator as StochasticResult),
            _ => false
        };
    }
    
    public bool Evaluate(IndicatorAlert alert, Dictionary<string, IndicatorResult> indicators)
    {
        return EvaluateAsync(alert, indicators).GetAwaiter().GetResult();
    }
    
    private async Task<bool> EvaluateMACDAlertAsync(IndicatorAlert alert, MACDResult? macd, CancellationToken cancellationToken)
    {
        if (macd == null) return false;
        
        // For crossing conditions, check history
        if ((alert.ConditionType == AlertConditionType.CrossesAbove || alert.ConditionType == AlertConditionType.CrossesBelow) 
            && _historyService != null && !string.IsNullOrEmpty(_symbol) && !string.IsNullOrEmpty(_timeframe))
        {
            var isCrossing = await _historyService.DetectMACDCrossingAsync(_symbol, _timeframe, macd, cancellationToken);
            if (isCrossing)
            {
                if (alert.ConditionType == AlertConditionType.CrossesAbove && macd.MACD > macd.Signal)
                    return true;
                if (alert.ConditionType == AlertConditionType.CrossesBelow && macd.MACD < macd.Signal)
                    return true;
            }
            return false;
        }
        
        var value = alert.TargetField switch
        {
            "MACD" => macd.MACD,
            "Signal" => macd.Signal,
            "Histogram" => macd.Histogram,
            _ => macd.MACD
        };
        
        return EvaluateCondition(alert.ConditionType, value, alert.Threshold ?? 0, macd.Signal);
    }
    
    private bool EvaluateRSIAlert(IndicatorAlert alert, RSIResult? rsi)
    {
        if (rsi == null) return false;
        
        return EvaluateCondition(alert.ConditionType, rsi.Value, alert.Threshold ?? 0, 0);
    }
    
    private bool EvaluateStochasticAlert(IndicatorAlert alert, StochasticResult? stoch)
    {
        if (stoch == null) return false;
        
        var value = alert.TargetField switch
        {
            "PercentK" => stoch.PercentK,
            "PercentD" => stoch.PercentD,
            _ => stoch.PercentK
        };
        
        return EvaluateCondition(alert.ConditionType, value, alert.Threshold ?? 0, stoch.PercentD);
    }
    
    private bool EvaluateCondition(AlertConditionType conditionType, decimal value, decimal threshold, decimal compareValue)
    {
        return conditionType switch
        {
            AlertConditionType.RisesAbove => value > threshold,
            AlertConditionType.FallsBelow => value < threshold,
            AlertConditionType.CrossesAbove => value > compareValue && value > threshold,
            AlertConditionType.CrossesBelow => value < compareValue && value < threshold,
            AlertConditionType.Equals => Math.Abs(value - threshold) < 0.01m,
            AlertConditionType.GreaterThan => value > threshold,
            AlertConditionType.LessThan => value < threshold,
            _ => false
        };
    }
}
