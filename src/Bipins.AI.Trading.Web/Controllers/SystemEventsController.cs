using Bipins.AI.Trading.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class SystemEventsController : Controller
{
    private readonly TradingDbContext _context;
    private readonly ILogger<SystemEventsController> _logger;
    
    public SystemEventsController(
        TradingDbContext context,
        ILogger<SystemEventsController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(
        string? eventType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AgentEvents.AsQueryable();
        
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType == eventType);
        
        if (from.HasValue)
            query = query.Where(e => e.EventTimestamp >= from.Value);
        
        if (to.HasValue)
            query = query.Where(e => e.EventTimestamp <= to.Value);
        
        var events = await query.OrderByDescending(e => e.EventTimestamp).ToListAsync(cancellationToken);
        return View(events);
    }
}
