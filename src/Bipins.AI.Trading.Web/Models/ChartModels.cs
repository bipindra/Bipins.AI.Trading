namespace Bipins.AI.Trading.Web.Models;

public class CandleDataDto
{
    public long Time { get; set; } // Unix timestamp in seconds
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class IndicatorDataPointDto
{
    public long Time { get; set; } // Unix timestamp in seconds
    public decimal Value { get; set; }
}

public class MacdDataPointDto
{
    public long Time { get; set; }
    public decimal Macd { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
}

public class ChartDataResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public List<CandleDataDto> Candles { get; set; } = new();
    public MacdDataDto? Macd { get; set; }
    public List<IndicatorDataPointDto>? Rsi { get; set; }
    public List<IndicatorDataPointDto>? MovingAverage { get; set; }
}

public class MacdDataDto
{
    public string Indicator { get; set; } = "MACD";
    public List<MacdDataPointDto> Data { get; set; } = new();
}

public class LatestCandleResponse
{
    public CandleDataDto? Candle { get; set; }
    public bool IsNew { get; set; }
}
