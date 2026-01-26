namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class IndicatorAlertEntity
{
    public long Id { get; set; }
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public string StrategyId { get; set; } = string.Empty;
    public string IndicatorType { get; set; } = string.Empty; // MACD, RSI, Stochastic
    public string ConditionType { get; set; } = string.Empty; // RisesAbove, FallsBelow, etc.
    public decimal? Threshold { get; set; }
    public string? TargetField { get; set; } // For MACD: "MACD", "Signal", "Histogram"
    public string Timeframe { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Buy, Sell, Hold
    public int Order { get; set; }
}
