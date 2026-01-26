using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class IndicatorsController : Controller
{
    private readonly IndicatorService _indicatorService;
    private readonly IndicatorRegistry _indicatorRegistry;
    private readonly ICandleRepository _candleRepository;
    private readonly ILogger<IndicatorsController> _logger;
    
    public IndicatorsController(
        IndicatorService indicatorService,
        IndicatorRegistry indicatorRegistry,
        ICandleRepository candleRepository,
        ILogger<IndicatorsController> logger)
    {
        _indicatorService = indicatorService;
        _indicatorRegistry = indicatorRegistry;
        _candleRepository = candleRepository;
        _logger = logger;
    }
    
    public IActionResult Index()
    {
        var indicators = _indicatorRegistry.GetAvailableIndicators();
        return Json(indicators);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetValues(
        string symbol,
        string timeframe,
        string indicator,
        CancellationToken cancellationToken)
    {
        try
        {
            var sym = new Symbol(symbol);
            var tf = new Timeframe(timeframe);
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;
            
            var candles = await _candleRepository.GetCandlesAsync(sym, tf, from, to, cancellationToken);
            
            if (candles.Count < 14)
            {
                return Json(new { error = "Insufficient candles" });
            }
            
            IndicatorResult? result = indicator switch
            {
                "MACD" => _indicatorService.Calculate<MACDResult>(indicator, candles),
                "RSI" => _indicatorService.Calculate<RSIResult>(indicator, candles),
                "Stochastic" => _indicatorService.Calculate<StochasticResult>(indicator, candles),
                _ => null
            };
            
            if (result == null)
            {
                return Json(new { error = "Unknown indicator" });
            }
            
            return Json(new
            {
                indicator = indicator,
                timestamp = result.Timestamp,
                values = GetIndicatorValues(result),
                metadata = result.Metadata
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator values");
            return Json(new { error = ex.Message });
        }
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
