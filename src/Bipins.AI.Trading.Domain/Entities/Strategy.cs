using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Strategy
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Timeframe Timeframe { get; set; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public List<IndicatorAlert> Alerts { get; set; } = new();
    public List<AlertCondition> Conditions { get; set; } = new();
    public TradeAction? FinalAction { get; set; } // Final action when all alert criteria are met
}
