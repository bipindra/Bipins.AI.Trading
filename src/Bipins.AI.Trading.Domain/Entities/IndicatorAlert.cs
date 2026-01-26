using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public enum IndicatorType
{
    MACD,
    RSI,
    Stochastic
}

public enum AlertConditionType
{
    RisesAbove,
    FallsBelow,
    CrossesAbove,
    CrossesBelow,
    Equals,
    GreaterThan,
    LessThan
}

public class IndicatorAlert
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string StrategyId { get; set; } = string.Empty;
    public IndicatorType IndicatorType { get; set; }
    public AlertConditionType ConditionType { get; set; }
    public decimal? Threshold { get; set; }
    public string? TargetField { get; set; } // For MACD: "MACD", "Signal", "Histogram"
    public Timeframe Timeframe { get; set; } = null!;
    public TradeAction Action { get; set; }
    public int Order { get; set; } // For ordering in UI
}
