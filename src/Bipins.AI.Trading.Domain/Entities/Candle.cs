using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Candle
{
    public Symbol Symbol { get; init; } = null!;
    public Timeframe Timeframe { get; init; } = null!;
    public DateTime Timestamp { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
    
    public decimal Range => High - Low;
    public decimal Body => Math.Abs(Close - Open);
    public bool IsBullish => Close > Open;
    public bool IsBearish => Close < Open;
    
    public string GetIdempotencyKey() => $"{Symbol.Value}_{Timeframe.Value}_{Timestamp:yyyyMMddHHmmss}";
}
