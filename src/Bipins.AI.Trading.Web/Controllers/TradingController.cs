using Bipins.AI.Trading.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class TradingController : Controller
{
    private readonly IOptionsMonitor<TradingOptions> _tradingOptionsMonitor;
    private readonly ILogger<TradingController> _logger;
    
    public TradingController(
        IOptionsMonitor<TradingOptions> tradingOptionsMonitor,
        ILogger<TradingController> logger)
    {
        _tradingOptionsMonitor = tradingOptionsMonitor;
        _logger = logger;
    }
    
    public IActionResult Index()
    {
        var options = _tradingOptionsMonitor.CurrentValue;
        return View(options);
    }
    
    [HttpPost]
    public IActionResult ToggleEnabled()
    {
        // Note: In production, this should update configuration store/database
        // For now, this is a placeholder - configuration changes require app restart
        _logger.LogWarning("ToggleEnabled called - configuration changes require app restart in current implementation");
        TempData["Message"] = "Trading enabled/disabled state requires application restart. Update appsettings.json and restart.";
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    public IActionResult SetMode(string mode)
    {
        // Note: In production, this should update configuration store/database
        _logger.LogWarning("SetMode called - configuration changes require app restart in current implementation");
        TempData["Message"] = "Trading mode changes require application restart. Update appsettings.json and restart.";
        return RedirectToAction(nameof(Index));
    }
}
