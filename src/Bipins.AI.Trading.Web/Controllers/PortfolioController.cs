using Bipins.AI.Trading.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class PortfolioController : Controller
{
    private readonly IPortfolioService _portfolioService;
    private readonly ILogger<PortfolioController> _logger;
    
    public PortfolioController(
        IPortfolioService portfolioService,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(cancellationToken);
        return View(portfolio);
    }
}
