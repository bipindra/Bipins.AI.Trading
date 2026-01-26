using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web;

public class FeatureComputeHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<FeatureComputeHostedService> _logger;
    
    public FeatureComputeHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<FeatureComputeHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FeatureComputeHostedService started");
        
        var timeframe = new Timeframe(_tradingOptions.Timeframe);
        var interval = timeframe.ToTimeSpan();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
                var indicatorService = scope.ServiceProvider.GetRequiredService<IndicatorService>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                
                foreach (var symbolStr in _tradingOptions.Symbols)
                {
                    var symbol = new Symbol(symbolStr);
                    var latestCandle = await candleRepository.GetLatestCandleAsync(symbol, timeframe, stoppingToken);
                    
                    if (latestCandle != null)
                    {
                        // Get historical candles for feature computation
                        var from = latestCandle.Timestamp.AddDays(-1);
                        var candles = await candleRepository.GetCandlesAsync(symbol, timeframe, from, latestCandle.Timestamp, stoppingToken);
                        
                        if (candles.Count >= 14) // Need at least 14 candles for RSI
                        {
                            // Compute simple features
                            var features = ComputeFeatures(candles);
                            
                            // Calculate indicators
                            var indicatorNames = new List<string> { "MACD", "RSI", "Stochastic" };
                            var indicators = indicatorService.CalculateAll(indicatorNames, candles);
                            
                            // Convert indicators to dictionary for contract
                            var indicatorsDict = indicators.ToDictionary(
                                kvp => kvp.Key,
                                kvp => new Dictionary<string, object>
                                {
                                    ["Timestamp"] = kvp.Value.Timestamp,
                                    ["Metadata"] = kvp.Value.Metadata
                                }.Concat(GetIndicatorValues(kvp.Value)).ToDictionary(x => x.Key, x => x.Value)
                            );
                            
                            var correlationId = Guid.NewGuid().ToString();
                            
                            // Publish FeaturesComputed (for backward compatibility)
                            await publishEndpoint.Publish(new FeaturesComputed(
                                symbol.Value,
                                timeframe.Value,
                                latestCandle.Timestamp,
                                features,
                                correlationId), stoppingToken);
                            
                            // Publish IndicatorsCalculated
                            await publishEndpoint.Publish(new IndicatorsCalculated(
                                symbol.Value,
                                timeframe.Value,
                                latestCandle.Timestamp,
                                indicatorsDict,
                                correlationId), stoppingToken);
                            
                            _logger.LogDebug("Published FeaturesComputed and IndicatorsCalculated for {Symbol} at {Timestamp}",
                                symbol.Value, latestCandle.Timestamp);
                        }
                    }
                }
                
                // Wait for next interval
                await Task.Delay(interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in feature computation");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
    
    private Dictionary<string, decimal> ComputeFeatures(List<Domain.Entities.Candle> candles)
    {
        var features = new Dictionary<string, decimal>();
        
        if (candles.Count < 2) return features;
        
        var latest = candles.Last();
        var prev = candles[candles.Count - 2];
        
        // Simple price change
        features["PriceChange"] = latest.Close - prev.Close;
        features["PriceChangePercent"] = prev.Close > 0 ? ((latest.Close - prev.Close) / prev.Close) * 100 : 0;
        
        // Volume change
        features["VolumeChange"] = latest.Volume - prev.Volume;
        
        // Range
        features["Range"] = latest.Range;
        features["Body"] = latest.Body;
        
        // High/Low ratios
        if (latest.High > 0)
        {
            features["CloseToHighRatio"] = latest.Close / latest.High;
        }
        if (latest.Low > 0)
        {
            features["CloseToLowRatio"] = latest.Close / latest.Low;
        }
        
        return features;
    }
    
    private Dictionary<string, object> GetIndicatorValues(IndicatorResult result)
    {
        return result switch
        {
            MACDResult macd => new Dictionary<string, object>
            {
                ["MACD"] = macd.MACD,
                ["Signal"] = macd.Signal,
                ["Histogram"] = macd.Histogram
            },
            RSIResult rsi => new Dictionary<string, object>
            {
                ["Value"] = rsi.Value
            },
            StochasticResult stoch => new Dictionary<string, object>
            {
                ["PercentK"] = stoch.PercentK,
                ["PercentD"] = stoch.PercentD
            },
            _ => new Dictionary<string, object>()
        };
    }
}
