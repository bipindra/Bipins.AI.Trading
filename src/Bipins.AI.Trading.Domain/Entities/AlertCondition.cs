namespace Bipins.AI.Trading.Domain.Entities;

public enum ConditionOperator
{
    And,
    Or
}

public class AlertCondition
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string StrategyId { get; set; } = string.Empty;
    public string? LeftAlertId { get; set; }
    public ConditionOperator Operator { get; set; }
    public string? RightAlertId { get; set; }
    public string? LeftConditionId { get; set; } // For nested conditions
    public string? RightConditionId { get; set; } // For nested conditions
    public TradeAction Action { get; set; }
    public int Order { get; set; }
}
