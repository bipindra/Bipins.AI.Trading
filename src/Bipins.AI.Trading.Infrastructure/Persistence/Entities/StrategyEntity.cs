namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class StrategyEntity
{
    public long Id { get; set; }
    public string StrategyId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public string? FinalAction { get; set; } // Buy, Sell, or null
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
