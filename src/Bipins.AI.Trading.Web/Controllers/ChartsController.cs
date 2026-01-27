using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
[Route("Charts")]
public class ChartsController : Controller
{
    private readonly IMarketDataClient _marketDataClient;
    private readonly ICandleRepository _candleRepository;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<ChartsController> _logger;

    public ChartsController(
        IMarketDataClient marketDataClient,
        ICandleRepository candleRepository,
        IOptions<TradingOptions> tradingOptions,
        ILogger<ChartsController> logger)
    {
        _marketDataClient = marketDataClient;
        _candleRepository = candleRepository;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Symbols = _tradingOptions.Symbols;
        ViewBag.DefaultSymbol = _tradingOptions.Symbols.FirstOrDefault() ?? "SPY";
        ViewBag.DefaultTimeframe = _tradingOptions.Timeframe;
        ViewBag.Timeframes = new[] { "1m", "5m", "15m", "1h", "1d" };
        
        return View();
    }

    [HttpGet("api/candles")]
    public async Task<ActionResult<ChartDataResponse>> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] string timeframe = "5m",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                _logger.LogWarning("Symbol parameter is missing or empty");
                return BadRequest(new { error = "Symbol parameter is required" });
            }

            // Default time range: last 7 days
            var toDate = to ?? DateTime.UtcNow;
            var fromDate = from ?? toDate.AddDays(-7);

            _logger.LogInformation("Requesting candles for {Symbol} {Timeframe} from {From} to {To}", 
                symbol, timeframe, fromDate, toDate);

            var symbolObj = new Symbol(symbol);
            var timeframeObj = new Timeframe(timeframe);

            // Try to get from repository first (cached/stored data)
            List<Domain.Entities.Candle> storedCandles;
            try
            {
                storedCandles = await _candleRepository.GetCandlesAsync(
                    symbolObj,
                    timeframeObj,
                    fromDate,
                    toDate,
                    cancellationToken);
                _logger.LogDebug("Repository returned {Count} candles for {Symbol} {Timeframe}",
                    storedCandles?.Count ?? 0, symbol, timeframe);
            }
            catch (Exception repoEx)
            {
                _logger.LogWarning(repoEx, "Error retrieving candles from repository, falling back to market data");
                storedCandles = new List<Domain.Entities.Candle>();
            }

            List<Domain.Entities.Candle> candles;

            if (storedCandles != null && storedCandles.Any())
            {
                candles = storedCandles;
                _logger.LogInformation("Using {Count} candles from repository for {Symbol} {Timeframe}",
                    candles.Count, symbol, timeframe);
            }
            else
            {
                // Fall back to market data client
                _logger.LogInformation("No candles in repository, fetching from market data client for {Symbol} {Timeframe}",
                    symbol, timeframe);
                
                try
                {
                    candles = await _marketDataClient.GetHistoricalCandlesAsync(
                        symbolObj,
                        timeframeObj,
                        fromDate,
                        toDate,
                        cancellationToken);
                    _logger.LogInformation("Retrieved {Count} candles from market data for {Symbol} {Timeframe}",
                        candles?.Count ?? 0, symbol, timeframe);
                }
                catch (Exception marketEx)
                {
                    _logger.LogError(marketEx, "Error retrieving candles from market data client");
                    candles = new List<Domain.Entities.Candle>();
                }
            }

            var candleDtos = candles.Select(c => new CandleDataDto
            {
                Time = ((DateTimeOffset)c.Timestamp).ToUnixTimeSeconds(),
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            }).OrderBy(c => c.Time).ToList();

            return Ok(new ChartDataResponse
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Candles = candleDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get candles for {Symbol} {Timeframe}. Error: {Error}", symbol, timeframe, ex.Message);
            return StatusCode(500, new { 
                error = "Failed to retrieve chart data",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("api/latest")]
    public async Task<ActionResult<LatestCandleResponse>> GetLatestCandle(
        [FromQuery] string symbol,
        [FromQuery] string timeframe = "5m",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var symbolObj = new Symbol(symbol);
            var timeframeObj = new Timeframe(timeframe);
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddHours(-1); // Get last hour to find latest candle

            var candles = await _marketDataClient.GetHistoricalCandlesAsync(
                symbolObj,
                timeframeObj,
                fromDate,
                toDate,
                cancellationToken);

            var latestCandle = candles.OrderByDescending(c => c.Timestamp).FirstOrDefault();

            if (latestCandle == null)
            {
                return Ok(new LatestCandleResponse { IsNew = false });
            }

            return Ok(new LatestCandleResponse
            {
                Candle = new CandleDataDto
                {
                    Time = ((DateTimeOffset)latestCandle.Timestamp).ToUnixTimeSeconds(),
                    Open = latestCandle.Open,
                    High = latestCandle.High,
                    Low = latestCandle.Low,
                    Close = latestCandle.Close,
                    Volume = latestCandle.Volume
                },
                IsNew = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest candle for {Symbol} {Timeframe}", symbol, timeframe);
            return StatusCode(500, new { error = "Failed to retrieve latest candle" });
        }
    }

    [HttpGet("api/indicators")]
    public async Task<ActionResult<object>> GetIndicators(
        [FromQuery] string symbol,
        [FromQuery] string timeframe = "5m",
        [FromQuery] string indicator = "MACD",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var toDate = to ?? DateTime.UtcNow;
            var fromDate = from ?? toDate.AddDays(-7);

            var symbolObj = new Symbol(symbol);
            var timeframeObj = new Timeframe(timeframe);

            // Get candles for indicator calculation
            var candles = await _candleRepository.GetCandlesAsync(
                symbolObj,
                timeframeObj,
                fromDate,
                toDate,
                cancellationToken);

            if (!candles.Any())
            {
                candles = await _marketDataClient.GetHistoricalCandlesAsync(
                    symbolObj,
                    timeframeObj,
                    fromDate,
                    toDate,
                    cancellationToken);
            }

            if (!candles.Any())
            {
                return Ok(new { indicator, data = new List<object>() });
            }

            // Calculate indicators based on type
            var orderedCandles = candles.OrderBy(c => c.Timestamp).ToList();

            return indicator.ToUpper() switch
            {
                "MACD" => Ok(CalculateMacd(orderedCandles)),
                "RSI" => Ok(CalculateRsi(orderedCandles)),
                "MA" => Ok(CalculateMovingAverage(orderedCandles, 20)),
                _ => BadRequest(new { error = $"Unknown indicator: {indicator}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator {Indicator} for {Symbol} {Timeframe}", indicator, symbol, timeframe);
            return StatusCode(500, new { error = "Failed to calculate indicator" });
        }
    }

    private MacdDataDto CalculateMacd(List<Domain.Entities.Candle> candles)
    {
        // Simplified MACD calculation (12, 26, 9)
        // In production, use proper indicator calculation service
        var data = new List<MacdDataPointDto>();
        
        if (candles.Count < 26)
        {
            return new MacdDataDto { Data = data };
        }

        // Calculate EMA12 and EMA26
        var ema12 = CalculateEMA(candles, 12);
        var ema26 = CalculateEMA(candles, 26);

        // Calculate MACD line (EMA12 - EMA26)
        var macdLine = new List<decimal>();
        for (int i = 0; i < Math.Min(ema12.Count, ema26.Count); i++)
        {
            macdLine.Add(ema12[i] - ema26[i]);
        }

        // Calculate Signal line (9-period EMA of MACD)
        var signalLine = CalculateEMAFromValues(macdLine, 9);

        // Calculate Histogram (MACD - Signal)
        // Align MACD data with candles (MACD starts after 26 candles, signal after additional 9)
        var macdStartIndex = 26 - 1; // First MACD value index in candles array
        var signalStartIndex = macdStartIndex + 9; // First signal value index
        
        // Use the longer of the two arrays to determine data points
        var maxLength = Math.Max(macdLine.Count, signalLine.Count);
        
        for (int i = 0; i < maxLength; i++)
        {
            var candleIndex = macdStartIndex + i;
            if (candleIndex >= 0 && candleIndex < candles.Count)
            {
                var candle = candles[candleIndex];
                var macdValue = i < macdLine.Count ? macdLine[i] : 0;
                var signalValue = i < signalLine.Count ? signalLine[i] : 0;
                
                data.Add(new MacdDataPointDto
                {
                    Time = ((DateTimeOffset)candle.Timestamp).ToUnixTimeSeconds(),
                    Macd = macdValue,
                    Signal = signalValue,
                    Histogram = macdValue - signalValue
                });
            }
        }

        return new MacdDataDto { Data = data };
    }

    private List<IndicatorDataPointDto> CalculateRsi(List<Domain.Entities.Candle> candles, int period = 14)
    {
        var data = new List<IndicatorDataPointDto>();
        
        if (candles.Count < period + 1)
        {
            return data;
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? -change : 0);
        }

        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        for (int i = period; i < candles.Count; i++)
        {
            if (avgLoss == 0)
            {
                data.Add(new IndicatorDataPointDto
                {
                    Time = ((DateTimeOffset)candles[i].Timestamp).ToUnixTimeSeconds(),
                    Value = 100
                });
                continue;
            }

            var rs = avgGain / avgLoss;
            var rsi = 100 - (100 / (1 + rs));

            data.Add(new IndicatorDataPointDto
            {
                Time = ((DateTimeOffset)candles[i].Timestamp).ToUnixTimeSeconds(),
                Value = rsi
            });

            // Update averages (simplified)
            if (i < candles.Count - 1)
            {
                avgGain = (avgGain * (period - 1) + gains[i]) / period;
                avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
            }
        }

        return data;
    }

    private List<IndicatorDataPointDto> CalculateMovingAverage(List<Domain.Entities.Candle> candles, int period)
    {
        var data = new List<IndicatorDataPointDto>();
        
        if (candles.Count < period)
        {
            return data;
        }

        for (int i = period - 1; i < candles.Count; i++)
        {
            var sum = candles.Skip(i - period + 1).Take(period).Sum(c => c.Close);
            var ma = sum / period;

            data.Add(new IndicatorDataPointDto
            {
                Time = ((DateTimeOffset)candles[i].Timestamp).ToUnixTimeSeconds(),
                Value = ma
            });
        }

        return data;
    }

    private List<decimal> CalculateEMA(List<Domain.Entities.Candle> candles, int period)
    {
        if (candles.Count < period)
        {
            return new List<decimal>();
        }

        var ema = new List<decimal>();
        var multiplier = 2.0m / (period + 1);

        // Start with SMA
        var sma = candles.Take(period).Average(c => c.Close);
        ema.Add(sma);

        // Calculate EMA for remaining candles
        for (int i = period; i < candles.Count; i++)
        {
            var value = (candles[i].Close - ema.Last()) * multiplier + ema.Last();
            ema.Add(value);
        }

        return ema;
    }

    private List<decimal> CalculateEMAFromValues(List<decimal> values, int period)
    {
        if (values.Count < period)
        {
            return new List<decimal>();
        }

        var ema = new List<decimal>();
        var multiplier = 2.0m / (period + 1);

        // Start with SMA
        var sma = values.Take(period).Average();
        ema.Add(sma);

        // Calculate EMA for remaining values
        for (int i = period; i < values.Count; i++)
        {
            var value = (values[i] - ema.Last()) * multiplier + ema.Last();
            ema.Add(value);
        }

        return ema;
    }
}
