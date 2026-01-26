using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Indicators;

public class RSICalculator : IIndicatorCalculator<RSIResult>
{
    public string Name => "RSI";
    
    public RSIResult Calculate(List<Candle> candles, Dictionary<string, object>? config = null)
    {
        var period = config?.ContainsKey("Period") == true ? Convert.ToInt32(config["Period"]) : 14;
        
        if (candles.Count < period + 1)
        {
            throw new InvalidOperationException($"Insufficient candles for RSI. Need at least {period + 1}, got {candles.Count}");
        }
        
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
        
        if (avgLoss == 0) return new RSIResult { Value = 100, Timestamp = candles.Last().Timestamp };
        
        var rs = avgGain / avgLoss;
        var rsi = 100m - (100m / (1 + rs));
        
        var latestCandle = candles.Last();
        
        return new RSIResult
        {
            Value = rsi,
            Timestamp = latestCandle.Timestamp,
            Metadata = new Dictionary<string, object>
            {
                ["Period"] = period
            }
        };
    }
    
    public bool CanCalculate(List<Candle> candles)
    {
        return candles.Count >= 15; // 14 + 1 minimum
    }
    
    public List<string> GetRequiredConfigKeys()
    {
        return new List<string> { "Period" };
    }
}
