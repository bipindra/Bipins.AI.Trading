namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class PortfolioSnapshotEntity
{
    public long Id { get; set; }
    public decimal Cash { get; set; }
    public decimal Equity { get; set; }
    public decimal BuyingPower { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public int PositionCount { get; set; }
    public DateTime SnapshotTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
