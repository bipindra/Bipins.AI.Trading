using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Services;

public class DecisionEngine : IDecisionEngine
{
    private readonly ILogger<DecisionEngine> _logger;
    
    public DecisionEngine(ILogger<DecisionEngine> logger)
    {
        _logger = logger;
    }
    
    public async Task<TradeDecision> MakeDecisionAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, decimal> features,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        if (candles.Count < 14)
        {
            return new TradeDecision
            {
                Symbol = symbol,
                Timeframe = timeframe,
                CandleTimestamp = candles.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Action = TradeAction.Hold,
                Confidence = 0.0m,
                Rationale = "Insufficient historical data",
                Features = features.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
            };
        }
        
        // Calculate RSI
        var rsi = CalculateRSI(candles, 14);
        
        // Calculate MACD
        var (macd, signal, histogram) = CalculateMACD(candles);
        
        // Get latest candle
        var latestCandle = candles.Last();
        
        // Simple strategy: RSI + MACD
        var action = TradeAction.Hold;
        var confidence = 0.5m;
        var rationale = new List<string>();
        
        // RSI signals
        if (rsi < 30)
        {
            action = TradeAction.Buy;
            confidence += 0.2m;
            rationale.Add($"RSI oversold ({rsi:F2})");
        }
        else if (rsi > 70)
        {
            action = TradeAction.Sell;
            confidence += 0.2m;
            rationale.Add($"RSI overbought ({rsi:F2})");
        }
        
        // MACD signals
        if (histogram > 0 && macd > signal)
        {
            if (action == TradeAction.Buy)
            {
                confidence += 0.2m;
                rationale.Add("MACD bullish crossover");
            }
            else if (action == TradeAction.Hold)
            {
                action = TradeAction.Buy;
                confidence = 0.4m;
                rationale.Add("MACD bullish");
            }
        }
        else if (histogram < 0 && macd < signal)
        {
            if (action == TradeAction.Sell)
            {
                confidence += 0.2m;
                rationale.Add("MACD bearish crossover");
            }
            else if (action == TradeAction.Hold)
            {
                action = TradeAction.Sell;
                confidence = 0.4m;
                rationale.Add("MACD bearish");
            }
        }
        
        // Check existing position
        var existingPosition = portfolio.GetPosition(symbol);
        if (existingPosition != null && !existingPosition.IsFlat)
        {
            if (action == TradeAction.Buy && existingPosition.IsLong)
            {
                action = TradeAction.Hold;
                rationale.Add("Already long");
                confidence = 0.3m;
            }
            else if (action == TradeAction.Sell && existingPosition.IsShort)
            {
                action = TradeAction.Hold;
                rationale.Add("Already short");
                confidence = 0.3m;
            }
        }
        
        // Calculate quantity (5% of portfolio for now, can be made configurable)
        decimal? quantityPercent = null;
        if (action != TradeAction.Hold && confidence > 0.5m)
        {
            quantityPercent = 5.0m; // 5% of portfolio
        }
        
        // Calculate stop loss and take profit (simple ATR-based)
        var atr = CalculateATR(candles, 14);
        Money? stopLoss = null;
        Money? takeProfit = null;
        
        if (action == TradeAction.Buy && latestCandle.Close > 0)
        {
            stopLoss = new Money(latestCandle.Close - (atr * 2));
            takeProfit = new Money(latestCandle.Close + (atr * 3));
        }
        else if (action == TradeAction.Sell && latestCandle.Close > 0)
        {
            stopLoss = new Money(latestCandle.Close + (atr * 2));
            takeProfit = new Money(latestCandle.Close - (atr * 3));
        }
        
        var decision = new TradeDecision
        {
            Symbol = symbol,
            Timeframe = timeframe,
            CandleTimestamp = latestCandle.Timestamp,
            Action = action,
            QuantityPercent = quantityPercent,
            SuggestedStopLoss = stopLoss,
            SuggestedTakeProfit = takeProfit,
            Confidence = Math.Min(confidence, 1.0m),
            Rationale = string.Join("; ", rationale),
            Features = new Dictionary<string, object>
            {
                ["RSI"] = rsi,
                ["MACD"] = macd,
                ["MACD_Signal"] = signal,
                ["MACD_Histogram"] = histogram,
                ["ATR"] = atr,
                ["Close"] = latestCandle.Close
            }
        };
        
        _logger.LogInformation("Decision made for {Symbol}: {Action} with confidence {Confidence:F2} - {Rationale}",
            symbol.Value, action, confidence, decision.Rationale);
        
        return decision;
    }
    
    private static decimal CalculateRSI(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1) return 50m;
        
        var gains = new List<decimal>();
        var losses = new List<decimal>();
        
        for (int i = candles.Count - period; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            if (change > 0)
            {
                gains.Add(change);
                losses.Add(0);
            }
            else
            {
                gains.Add(0);
                losses.Add(Math.Abs(change));
            }
        }
        
        var avgGain = gains.Average();
        var avgLoss = losses.Average();
        
        if (avgLoss == 0) return 100m;
        
        var rs = avgGain / avgLoss;
        return 100m - (100m / (1 + rs));
    }
    
    private static (decimal macd, decimal signal, decimal histogram) CalculateMACD(List<Candle> candles)
    {
        if (candles.Count < 26) return (0, 0, 0);
        
        var ema12 = CalculateEMA(candles.Select(c => c.Close).ToList(), 12);
        var ema26 = CalculateEMA(candles.Select(c => c.Close).ToList(), 26);
        var macd = ema12 - ema26;
        
        // Signal line (9-period EMA of MACD)
        var macdValues = new List<decimal>();
        for (int i = candles.Count - 9; i < candles.Count; i++)
        {
            var shortEma = CalculateEMA(candles.Skip(i - 11).Take(12).Select(c => c.Close).ToList(), 12);
            var longEma = CalculateEMA(candles.Skip(i - 25).Take(26).Select(c => c.Close).ToList(), 26);
            macdValues.Add(shortEma - longEma);
        }
        
        var signal = CalculateEMA(macdValues, 9);
        var histogram = macd - signal;
        
        return (macd, signal, histogram);
    }
    
    private static decimal CalculateEMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.LastOrDefault();
        
        var multiplier = 2.0m / (period + 1);
        var ema = values.Take(period).Average();
        
        foreach (var value in values.Skip(period))
        {
            ema = (value * multiplier) + (ema * (1 - multiplier));
        }
        
        return ema;
    }
    
    private static decimal CalculateATR(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1) return 0;
        
        var trueRanges = new List<decimal>();
        for (int i = candles.Count - period; i < candles.Count; i++)
        {
            var high = candles[i].High;
            var low = candles[i].Low;
            var prevClose = i > 0 ? candles[i - 1].Close : candles[i].Open;
            
            var tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
            trueRanges.Add(tr);
        }
        
        return trueRanges.Average();
    }
}
