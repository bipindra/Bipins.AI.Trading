using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Indicators;

public class StochasticCalculator : IIndicatorCalculator<StochasticResult>
{
    public string Name => "Stochastic";
    
    public StochasticResult Calculate(List<Candle> candles, Dictionary<string, object>? config = null)
    {
        var kPeriod = config?.ContainsKey("KPeriod") == true ? Convert.ToInt32(config["KPeriod"]) : 14;
        var dPeriod = config?.ContainsKey("DPeriod") == true ? Convert.ToInt32(config["DPeriod"]) : 3;
        var smoothing = config?.ContainsKey("Smoothing") == true ? Convert.ToInt32(config["Smoothing"]) : 3;
        
        if (candles.Count < kPeriod + dPeriod)
        {
            throw new InvalidOperationException($"Insufficient candles for Stochastic. Need at least {kPeriod + dPeriod}, got {candles.Count}");
        }
        
        // Calculate %K values
        var percentKValues = new List<decimal>();
        for (int i = kPeriod - 1; i < candles.Count; i++)
        {
            var periodCandles = candles.Skip(i - kPeriod + 1).Take(kPeriod).ToList();
            var highestHigh = periodCandles.Max(c => c.High);
            var lowestLow = periodCandles.Min(c => c.Low);
            var currentClose = candles[i].Close;
            
            if (highestHigh == lowestLow)
            {
                percentKValues.Add(50); // Neutral when no range
            }
            else
            {
                var percentK = ((currentClose - lowestLow) / (highestHigh - lowestLow)) * 100;
                percentKValues.Add(percentK);
            }
        }
        
        // Apply smoothing to %K
        var smoothedK = new List<decimal>();
        for (int i = 0; i < percentKValues.Count; i++)
        {
            var start = Math.Max(0, i - smoothing + 1);
            var kValues = percentKValues.Skip(start).Take(Math.Min(smoothing, i + 1)).ToList();
            smoothedK.Add(kValues.Average());
        }
        
        // Calculate %D (moving average of smoothed %K)
        var percentD = new List<decimal>();
        for (int i = dPeriod - 1; i < smoothedK.Count; i++)
        {
            var dValues = smoothedK.Skip(i - dPeriod + 1).Take(dPeriod).ToList();
            percentD.Add(dValues.Average());
        }
        
        var latestCandle = candles.Last();
        var latestK = smoothedK.LastOrDefault();
        var latestD = percentD.LastOrDefault();
        
        return new StochasticResult
        {
            PercentK = latestK,
            PercentD = latestD,
            Timestamp = latestCandle.Timestamp,
            Metadata = new Dictionary<string, object>
            {
                ["KPeriod"] = kPeriod,
                ["DPeriod"] = dPeriod,
                ["Smoothing"] = smoothing
            }
        };
    }
    
    public bool CanCalculate(List<Candle> candles)
    {
        return candles.Count >= 17; // 14 (K) + 3 (D) minimum
    }
    
    public List<string> GetRequiredConfigKeys()
    {
        return new List<string> { "KPeriod", "DPeriod", "Smoothing" };
    }
}
