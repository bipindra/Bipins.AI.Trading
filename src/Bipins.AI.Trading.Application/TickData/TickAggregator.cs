using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.TickData;

public class TickAggregator
{
    public List<Candle> AggregateToCandles(
        List<Tick> ticks,
        Symbol symbol,
        Timeframe timeframe)
    {
        if (!ticks.Any()) return new List<Candle>();
        
        var candles = new List<Candle>();
        var groupedTicks = ticks
            .OrderBy(t => t.Timestamp)
            .GroupBy(t => GetCandleTimestamp(t.Timestamp, timeframe));
        
        foreach (var group in groupedTicks)
        {
            var ticksInCandle = group.OrderBy(t => t.Timestamp).ToList();
            if (!ticksInCandle.Any()) continue;
            
            var open = ticksInCandle.First().Price;
            var close = ticksInCandle.Last().Price;
            var high = ticksInCandle.Max(t => t.Price);
            var low = ticksInCandle.Min(t => t.Price);
            var volume = ticksInCandle.Sum(t => t.Volume);
            
            candles.Add(new Candle
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Timestamp = group.Key,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });
        }
        
        return candles.OrderBy(c => c.Timestamp).ToList();
    }
    
    private DateTime GetCandleTimestamp(DateTime tickTimestamp, Timeframe timeframe)
    {
        var timeSpan = timeframe.ToTimeSpan();
        
        return timeframe.Value switch
        {
            "1m" => new DateTime(tickTimestamp.Year, tickTimestamp.Month, tickTimestamp.Day, tickTimestamp.Hour, tickTimestamp.Minute, 0, DateTimeKind.Utc),
            "5m" => new DateTime(tickTimestamp.Year, tickTimestamp.Month, tickTimestamp.Day, tickTimestamp.Hour, tickTimestamp.Minute / 5 * 5, 0, DateTimeKind.Utc),
            "15m" => new DateTime(tickTimestamp.Year, tickTimestamp.Month, tickTimestamp.Day, tickTimestamp.Hour, tickTimestamp.Minute / 15 * 15, 0, DateTimeKind.Utc),
            "1h" => new DateTime(tickTimestamp.Year, tickTimestamp.Month, tickTimestamp.Day, tickTimestamp.Hour, 0, 0, DateTimeKind.Utc),
            "1d" => new DateTime(tickTimestamp.Year, tickTimestamp.Month, tickTimestamp.Day, 0, 0, 0, DateTimeKind.Utc),
            _ => tickTimestamp
        };
    }
}
