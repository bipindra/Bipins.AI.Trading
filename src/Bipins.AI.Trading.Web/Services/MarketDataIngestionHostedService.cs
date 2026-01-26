using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web;

public class MarketDataIngestionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<MarketDataIngestionHostedService> _logger;
    
    public MarketDataIngestionHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<MarketDataIngestionHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketDataIngestionHostedService started");
        
        var symbols = _tradingOptions.Symbols.Select(s => new Symbol(s)).ToList();
        var timeframe = new Timeframe(_tradingOptions.Timeframe);
        var interval = timeframe.ToTimeSpan();
        
        DateTime? lastPollTime = null;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var marketDataClient = scope.ServiceProvider.GetRequiredService<IMarketDataClient>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var clock = scope.ServiceProvider.GetRequiredService<IClock>();
                
                var now = clock.UtcNow;
                var since = lastPollTime ?? now.AddHours(-1);
                
                var candles = await marketDataClient.PollLatestCandlesAsync(
                    symbols, timeframe, since, stoppingToken);
                
                foreach (var candle in candles)
                {
                    // Only publish if this is a new candle (not already processed)
                    if (lastPollTime == null || candle.Timestamp > lastPollTime.Value)
                    {
                        var correlationId = Guid.NewGuid().ToString();
                        
                        await publishEndpoint.Publish(new CandleClosed(
                            candle.Symbol.Value,
                            candle.Timeframe.Value,
                            candle.Timestamp,
                            candle.Open,
                            candle.High,
                            candle.Low,
                            candle.Close,
                            candle.Volume,
                            correlationId), stoppingToken);
                        
                        _logger.LogInformation("Published CandleClosed: {Symbol} {Timeframe} at {Timestamp}",
                            candle.Symbol.Value, candle.Timeframe.Value, candle.Timestamp);
                    }
                }
                
                if (candles.Any())
                {
                    lastPollTime = candles.Max(c => c.Timestamp);
                }
                
                // Wait for next interval
                await Task.Delay(interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in market data ingestion");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
