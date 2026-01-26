using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IPortfolioService _portfolioService;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<DashboardController> _logger;
    
    public DashboardController(
        IPortfolioService portfolioService,
        IOptions<TradingOptions> tradingOptions,
        ILogger<DashboardController> logger)
    {
        _portfolioService = portfolioService;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(cancellationToken);
        
        ViewBag.TradingEnabled = _tradingOptions.Enabled;
        ViewBag.TradingMode = _tradingOptions.Mode.ToString();
        ViewBag.LastUpdated = portfolio.LastUpdatedAt;
        
        return View(portfolio);
    }
}
