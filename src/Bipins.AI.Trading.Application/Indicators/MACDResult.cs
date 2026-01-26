namespace Bipins.AI.Trading.Application.Indicators;

public class MACDResult : IndicatorResult
{
    public decimal MACD { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
}
