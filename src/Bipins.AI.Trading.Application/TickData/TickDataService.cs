using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.TickData;

public class TickDataService
{
    private readonly ITickDataProvider _tickDataProvider;
    private readonly TickAggregator _aggregator;
    private readonly ILogger<TickDataService> _logger;
    
    public TickDataService(
        ITickDataProvider tickDataProvider,
        TickAggregator aggregator,
        ILogger<TickDataService> logger)
    {
        _tickDataProvider = tickDataProvider;
        _aggregator = aggregator;
        _logger = logger;
    }
    
    public async Task<List<Candle>> GetCandlesFromTicksAsync(
        Symbol symbol,
        Timeframe timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var ticks = await _tickDataProvider.GetTicksAsync(symbol, from, to, cancellationToken);
        return _aggregator.AggregateToCandles(ticks, symbol, timeframe);
    }
    
    public async Task SubscribeTicksAsync(
        Symbol symbol,
        Timeframe timeframe,
        Func<Candle, Task> onCandleComplete,
        CancellationToken cancellationToken = default)
    {
        var pendingTicks = new List<Tick>();
        var lastCandleTimestamp = DateTime.MinValue;
        
        await _tickDataProvider.SubscribeTicksAsync(symbol, async tick =>
        {
            pendingTicks.Add(tick);
            
            var candleTimestamp = GetCandleTimestamp(tick.Timestamp, timeframe);
            if (candleTimestamp > lastCandleTimestamp && pendingTicks.Any())
            {
                var candles = _aggregator.AggregateToCandles(pendingTicks, symbol, timeframe);
                foreach (var candle in candles)
                {
                    await onCandleComplete(candle);
                }
                
                pendingTicks.Clear();
                lastCandleTimestamp = candleTimestamp;
            }
        }, cancellationToken);
    }
    
    private DateTime GetCandleTimestamp(DateTime tickTimestamp, Timeframe timeframe)
    {
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
