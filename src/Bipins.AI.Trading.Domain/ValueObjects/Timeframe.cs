namespace Bipins.AI.Trading.Domain.ValueObjects;

public record Timeframe(string Value)
{
    public static readonly Timeframe OneMinute = new("1m");
    public static readonly Timeframe FiveMinute = new("5m");
    public static readonly Timeframe FifteenMinute = new("15m");
    public static readonly Timeframe OneHour = new("1h");
    public static readonly Timeframe OneDay = new("1d");
    
    public static implicit operator string(Timeframe timeframe) => timeframe.Value;
    public static implicit operator Timeframe(string value) => new(value);
    
    public TimeSpan ToTimeSpan()
    {
        return Value switch
        {
            "1m" => TimeSpan.FromMinutes(1),
            "5m" => TimeSpan.FromMinutes(5),
            "15m" => TimeSpan.FromMinutes(15),
            "1h" => TimeSpan.FromHours(1),
            "1d" => TimeSpan.FromDays(1),
            _ => throw new ArgumentException($"Unknown timeframe: {Value}")
        };
    }
    
    public override string ToString() => Value;
}
