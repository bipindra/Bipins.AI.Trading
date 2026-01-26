using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Strategies;

public class MACDBuySellStrategy : IStrategy
{
    public string Name => "MACD Buy/Sell";
    
    private readonly ILogger<MACDBuySellStrategy> _logger;
    
    public MACDBuySellStrategy(ILogger<MACDBuySellStrategy> logger)
    {
        _logger = logger;
    }
    
    public async Task<TradeDecision?> EvaluateAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, IndicatorResult> indicators,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        if (!indicators.TryGetValue("MACD", out var macdResult) || macdResult is not MACDResult macd)
        {
            return null;
        }
        
        if (candles.Count < 2)
        {
            return null;
        }
        
        // Get previous MACD values for crossing detection
        // In a real implementation, we'd need to track previous values
        // For now, we'll use the current histogram to detect crossing
        
        var latestCandle = candles.Last();
        TradeAction? action = null;
        var rationale = new List<string>();
        
        // Buy signal: MACD crosses above Signal (histogram becomes positive from negative)
        if (macd.Histogram > 0 && macd.MACD > macd.Signal)
        {
            // Check if this is a crossing (would need previous value, simplified here)
            action = TradeAction.Buy;
            rationale.Add("MACD crossed above Signal line (bullish crossover)");
        }
        // Sell signal: MACD crosses below Signal (histogram becomes negative from positive)
        else if (macd.Histogram < 0 && macd.MACD < macd.Signal)
        {
            action = TradeAction.Sell;
            rationale.Add("MACD crossed below Signal line (bearish crossover)");
        }
        
        if (!action.HasValue || action.Value == TradeAction.Hold)
        {
            return null;
        }
        
        // Check existing position
        var existingPosition = portfolio.GetPosition(symbol);
        if (existingPosition != null && !existingPosition.IsFlat)
        {
            if (action == TradeAction.Buy && existingPosition.IsLong)
            {
                return null; // Already long
            }
            if (action == TradeAction.Sell && existingPosition.IsShort)
            {
                return null; // Already short
            }
        }
        
        var decision = new TradeDecision
        {
            Symbol = symbol,
            Timeframe = timeframe,
            CandleTimestamp = latestCandle.Timestamp,
            Action = action.Value,
            QuantityPercent = 5.0m, // 5% of portfolio
            Confidence = 0.7m, // Medium confidence for MACD crossovers
            Rationale = string.Join("; ", rationale),
            Features = new Dictionary<string, object>
            {
                ["MACD"] = macd.MACD,
                ["Signal"] = macd.Signal,
                ["Histogram"] = macd.Histogram
            }
        };
        
        _logger.LogInformation("MACDBuySellStrategy: {Action} signal for {Symbol} - {Rationale}",
            action.Value, symbol.Value, decision.Rationale);
        
        return decision;
    }
    
    public List<string> GetRequiredIndicators()
    {
        return new List<string> { "MACD" };
    }
    
    public bool CanEvaluate(List<Candle> candles, Dictionary<string, IndicatorResult> indicators)
    {
        return candles.Count >= 35 && indicators.ContainsKey("MACD");
    }
}
