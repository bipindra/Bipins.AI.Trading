using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Tick
{
    public Symbol Symbol { get; init; } = null!;
    public DateTime Timestamp { get; init; }
    public decimal Price { get; init; }
    public long Volume { get; init; }
    public decimal? Bid { get; init; }
    public decimal? Ask { get; init; }
    public decimal? BidSize { get; init; }
    public decimal? AskSize { get; init; }
    
    public decimal? Spread => Ask.HasValue && Bid.HasValue ? Ask.Value - Bid.Value : null;
}
