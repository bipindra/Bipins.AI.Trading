namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class CandleEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
