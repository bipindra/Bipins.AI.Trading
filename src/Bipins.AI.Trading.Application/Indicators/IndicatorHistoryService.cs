using System.Text.Json;
using Bipins.AI.Trading.Application.Ports;

namespace Bipins.AI.Trading.Application.Indicators;

public class IndicatorHistoryService
{
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<IndicatorHistoryService> _logger;
    
    public IndicatorHistoryService(ICacheStore cacheStore, ILogger<IndicatorHistoryService> logger)
    {
        _cacheStore = cacheStore;
        _logger = logger;
    }
    
    public async Task<MACDResult?> GetPreviousMACDAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        var key = $"indicator:{symbol}:{timeframe}:MACD:previous";
        var json = await _cacheStore.GetAsync<string>(key, cancellationToken);
        if (string.IsNullOrEmpty(json)) return null;
        
        try
        {
            return JsonSerializer.Deserialize<MACDResult>(json);
        }
        catch
        {
            return null;
        }
    }
    
    public async Task SaveIndicatorAsync(
        string symbol,
        string timeframe,
        string indicatorName,
        IndicatorResult result,
        CancellationToken cancellationToken = default)
    {
        var key = $"indicator:{symbol}:{timeframe}:{indicatorName}:current";
        var previousKey = $"indicator:{symbol}:{timeframe}:{indicatorName}:previous";
        
        // Get current as previous
        var currentJson = await _cacheStore.GetAsync<string>(key, cancellationToken);
        if (!string.IsNullOrEmpty(currentJson))
        {
            await _cacheStore.SetAsync(previousKey, currentJson, TimeSpan.FromHours(24), cancellationToken);
        }
        
        // Save new as current (serialize to JSON)
        var json = JsonSerializer.Serialize(result);
        await _cacheStore.SetAsync(key, json, TimeSpan.FromHours(24), cancellationToken);
    }
    
    public async Task<bool> DetectMACDCrossingAsync(
        string symbol,
        string timeframe,
        MACDResult current,
        CancellationToken cancellationToken = default)
    {
        var previous = await GetPreviousMACDAsync(symbol, timeframe, cancellationToken);
        
        if (previous == null) return false;
        
        // Bullish crossover: MACD crosses above Signal
        var bullishCross = previous.MACD <= previous.Signal && current.MACD > current.Signal;
        
        // Bearish crossover: MACD crosses below Signal
        var bearishCross = previous.MACD >= previous.Signal && current.MACD < current.Signal;
        
        return bullishCross || bearishCross;
    }
}
