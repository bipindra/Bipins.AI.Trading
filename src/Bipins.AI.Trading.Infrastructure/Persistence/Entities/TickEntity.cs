namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class TickEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public decimal? Bid { get; set; }
    public decimal? Ask { get; set; }
    public decimal? BidSize { get; set; }
    public decimal? AskSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
