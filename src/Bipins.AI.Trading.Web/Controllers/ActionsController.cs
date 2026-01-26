using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Options;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class ActionsController : Controller
{
    private readonly TradingOptions _tradingOptions;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ActionsController> _logger;
    
    // In-memory store for pending actions (in production, use database)
    private static readonly Dictionary<string, ActionRequired> PendingActions = new();
    
    public ActionsController(
        IOptions<TradingOptions> tradingOptions,
        IPublishEndpoint publishEndpoint,
        ILogger<ActionsController> logger)
    {
        _tradingOptions = tradingOptions.Value;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public IActionResult Index()
    {
        var actions = PendingActions.Values.OrderByDescending(a => a.RequestedAt).ToList();
        return View(actions);
    }
    
    [HttpPost]
    public async Task<IActionResult> Approve(string decisionId, CancellationToken cancellationToken)
    {
        if (!PendingActions.TryGetValue(decisionId, out var action))
        {
            return NotFound();
        }
        
        PendingActions.Remove(decisionId);
        
        var correlationId = Guid.NewGuid().ToString();
        await _publishEndpoint.Publish(new TradeApproved(
            action.DecisionId,
            action.Symbol,
            action.Action,
            action.Quantity,
            null, // stop loss would come from decision
            User.Identity?.Name ?? "User",
            DateTime.UtcNow,
            correlationId), cancellationToken);
        
        _logger.LogInformation("User {User} approved trade {DecisionId}", User.Identity?.Name, decisionId);
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    public async Task<IActionResult> Reject(string decisionId, CancellationToken cancellationToken)
    {
        if (!PendingActions.TryGetValue(decisionId, out var action))
        {
            return NotFound();
        }
        
        PendingActions.Remove(decisionId);
        
        var correlationId = Guid.NewGuid().ToString();
        await _publishEndpoint.Publish(new TradeRejected(
            action.DecisionId,
            action.Symbol,
            "Rejected by user",
            correlationId), cancellationToken);
        
        _logger.LogInformation("User {User} rejected trade {DecisionId}", User.Identity?.Name, decisionId);
        
        return RedirectToAction(nameof(Index));
    }
    
    // Called by consumer when ActionRequired is published
    public static void AddPendingAction(ActionRequired action)
    {
        PendingActions[action.DecisionId] = action;
    }
}
