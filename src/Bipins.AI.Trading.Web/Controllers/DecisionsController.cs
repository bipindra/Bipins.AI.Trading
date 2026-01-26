using Bipins.AI.Trading.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class DecisionsController : Controller
{
    private readonly ITradeDecisionRepository _decisionRepository;
    private readonly ILogger<DecisionsController> _logger;
    
    public DecisionsController(
        ITradeDecisionRepository decisionRepository,
        ILogger<DecisionsController> logger)
    {
        _decisionRepository = decisionRepository;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(
        string? symbol = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var decisions = await _decisionRepository.GetDecisionsAsync(symbol, from, to, cancellationToken);
        return View(decisions);
    }
    
    public async Task<IActionResult> Export(
        string? symbol = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var decisions = await _decisionRepository.GetDecisionsAsync(symbol, from, to, cancellationToken);
        
        var csv = "Symbol,Timeframe,CandleTimestamp,Action,QuantityPercent,Confidence,Rationale\n";
        foreach (var decision in decisions)
        {
            csv += $"{decision.Symbol.Value},{decision.Timeframe.Value},{decision.CandleTimestamp:yyyy-MM-dd HH:mm:ss}," +
                   $"{decision.Action},{decision.QuantityPercent},{decision.Confidence:F2},\"{decision.Rationale}\"\n";
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"decisions_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
