using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Indicators;

public class MACDCalculator : IIndicatorCalculator<MACDResult>
{
    public string Name => "MACD";
    
    public MACDResult Calculate(List<Candle> candles, Dictionary<string, object>? config = null)
    {
        var fastPeriod = config?.ContainsKey("FastPeriod") == true ? Convert.ToInt32(config["FastPeriod"]) : 12;
        var slowPeriod = config?.ContainsKey("SlowPeriod") == true ? Convert.ToInt32(config["SlowPeriod"]) : 26;
        var signalPeriod = config?.ContainsKey("SignalPeriod") == true ? Convert.ToInt32(config["SignalPeriod"]) : 9;
        
        if (candles.Count < slowPeriod + signalPeriod)
        {
            throw new InvalidOperationException($"Insufficient candles for MACD. Need at least {slowPeriod + signalPeriod}, got {candles.Count}");
        }
        
        var closes = candles.Select(c => c.Close).ToList();
        var fastEMA = CalculateEMA(closes, fastPeriod);
        var slowEMA = CalculateEMA(closes, slowPeriod);
        var macdLine = fastEMA - slowEMA;
        
        // Calculate signal line (EMA of MACD line)
        var macdValues = new List<decimal>();
        for (int i = 0; i < closes.Count; i++)
        {
            if (i >= slowPeriod - 1)
            {
                var fast = CalculateEMA(closes.Take(i + 1).ToList(), fastPeriod);
                var slow = CalculateEMA(closes.Take(i + 1).ToList(), slowPeriod);
                macdValues.Add(fast - slow);
            }
        }
        
        var signalLine = CalculateEMA(macdValues, signalPeriod);
        var histogram = macdLine - signalLine;
        
        var latestCandle = candles.Last();
        
        return new MACDResult
        {
            MACD = macdLine,
            Signal = signalLine,
            Histogram = histogram,
            Timestamp = latestCandle.Timestamp,
            Metadata = new Dictionary<string, object>
            {
                ["FastPeriod"] = fastPeriod,
                ["SlowPeriod"] = slowPeriod,
                ["SignalPeriod"] = signalPeriod
            }
        };
    }
    
    public bool CanCalculate(List<Candle> candles)
    {
        return candles.Count >= 35; // 26 (slow) + 9 (signal) minimum
    }
    
    public List<string> GetRequiredConfigKeys()
    {
        return new List<string> { "FastPeriod", "SlowPeriod", "SignalPeriod" };
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
}
