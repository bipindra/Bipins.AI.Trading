namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class AlertConditionEntity
{
    public long Id { get; set; }
    public string ConditionId { get; set; } = Guid.NewGuid().ToString();
    public string StrategyId { get; set; } = string.Empty;
    public string? LeftAlertId { get; set; }
    public string Operator { get; set; } = string.Empty; // And, Or
    public string? RightAlertId { get; set; }
    public string? LeftConditionId { get; set; } // For nested conditions
    public string? RightConditionId { get; set; } // For nested conditions
    public string Action { get; set; } = string.Empty; // Buy, Sell, Hold
    public int Order { get; set; }
}
