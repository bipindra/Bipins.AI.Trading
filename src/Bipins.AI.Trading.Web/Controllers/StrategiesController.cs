using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Application.Strategies;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class StrategiesController : Controller
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly IndicatorService _indicatorService;
    private readonly StrategyExecutor _strategyExecutor;
    private readonly IPortfolioService _portfolioService;
    private readonly ILogger<StrategiesController> _logger;
    
    public StrategiesController(
        IStrategyRepository strategyRepository,
        ICandleRepository candleRepository,
        IndicatorService indicatorService,
        StrategyExecutor strategyExecutor,
        IPortfolioService portfolioService,
        ILogger<StrategiesController> logger)
    {
        _strategyRepository = strategyRepository;
        _candleRepository = candleRepository;
        _indicatorService = indicatorService;
        _strategyExecutor = strategyExecutor;
        _portfolioService = portfolioService;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var strategies = await _strategyRepository.GetAllAsync(cancellationToken);
        return View(strategies);
    }
    
    public IActionResult Create()
    {
        return View(new Strategy
        {
            Timeframe = new Timeframe("5m"),
            Alerts = new List<IndicatorAlert>(),
            Conditions = new List<AlertCondition>()
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(Strategy strategy, string? alertsJson, string? conditionsJson, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            // Parse alerts from JSON
            if (!string.IsNullOrEmpty(alertsJson))
            {
                var alertsData = System.Text.Json.JsonSerializer.Deserialize<List<AlertData>>(alertsJson);
                if (alertsData != null)
                {
                    strategy.Alerts = alertsData.Select((a, index) => new IndicatorAlert
                    {
                        Id = a.Id ?? Guid.NewGuid().ToString(),
                        StrategyId = strategy.Id,
                        IndicatorType = Enum.Parse<IndicatorType>(a.IndicatorType),
                        ConditionType = Enum.Parse<AlertConditionType>(a.ConditionType),
                        Threshold = a.Threshold,
                        TargetField = a.TargetField,
                        Timeframe = strategy.Timeframe,
                        Action = Enum.Parse<TradeAction>(a.Action),
                        Order = index
                    }).ToList();
                }
            }
            
            // Parse conditions from JSON
            if (!string.IsNullOrEmpty(conditionsJson))
            {
                var conditionsData = System.Text.Json.JsonSerializer.Deserialize<List<ConditionData>>(conditionsJson);
                if (conditionsData != null)
                {
                    strategy.Conditions = conditionsData.Select((c, index) => new AlertCondition
                    {
                        Id = c.Id ?? Guid.NewGuid().ToString(),
                        StrategyId = strategy.Id,
                        LeftAlertId = c.LeftAlertId,
                        Operator = Enum.Parse<ConditionOperator>(c.Operator),
                        RightAlertId = c.RightAlertId,
                        Action = Enum.Parse<TradeAction>(c.Action),
                        Order = index
                    }).ToList();
                }
            }
            
            await _strategyRepository.AddAsync(strategy, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        
        return View(strategy);
    }
    
    private class AlertData
    {
        public string? Id { get; set; }
        public string IndicatorType { get; set; } = string.Empty;
        public string ConditionType { get; set; } = string.Empty;
        public decimal? Threshold { get; set; }
        public string? TargetField { get; set; }
        public string Action { get; set; } = string.Empty;
    }
    
    private class ConditionData
    {
        public string? Id { get; set; }
        public string? LeftAlertId { get; set; }
        public string Operator { get; set; } = string.Empty;
        public string? RightAlertId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
    
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var strategy = await _strategyRepository.GetByIdAsync(id, cancellationToken);
        if (strategy == null)
        {
            return NotFound();
        }
        
        return View(strategy);
    }
    
    [HttpPost]
    public async Task<IActionResult> Edit(Strategy strategy, string? alertsJson, string? conditionsJson, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            // Parse alerts from JSON
            if (!string.IsNullOrEmpty(alertsJson))
            {
                var alertsData = System.Text.Json.JsonSerializer.Deserialize<List<AlertData>>(alertsJson);
                if (alertsData != null)
                {
                    strategy.Alerts = alertsData.Select((a, index) => new IndicatorAlert
                    {
                        Id = a.Id ?? Guid.NewGuid().ToString(),
                        StrategyId = strategy.Id,
                        IndicatorType = Enum.Parse<IndicatorType>(a.IndicatorType),
                        ConditionType = Enum.Parse<AlertConditionType>(a.ConditionType),
                        Threshold = a.Threshold,
                        TargetField = a.TargetField,
                        Timeframe = strategy.Timeframe,
                        Action = Enum.Parse<TradeAction>(a.Action),
                        Order = index
                    }).ToList();
                }
            }
            
            // Parse conditions from JSON
            if (!string.IsNullOrEmpty(conditionsJson))
            {
                var conditionsData = System.Text.Json.JsonSerializer.Deserialize<List<ConditionData>>(conditionsJson);
                if (conditionsData != null)
                {
                    strategy.Conditions = conditionsData.Select((c, index) => new AlertCondition
                    {
                        Id = c.Id ?? Guid.NewGuid().ToString(),
                        StrategyId = strategy.Id,
                        LeftAlertId = c.LeftAlertId,
                        Operator = Enum.Parse<ConditionOperator>(c.Operator),
                        RightAlertId = c.RightAlertId,
                        Action = Enum.Parse<TradeAction>(c.Action),
                        Order = index
                    }).ToList();
                }
            }
            
            strategy.UpdatedAt = DateTime.UtcNow;
            await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        
        return View(strategy);
    }
    
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var strategy = await _strategyRepository.GetByIdAsync(id, cancellationToken);
        if (strategy == null)
        {
            return NotFound();
        }
        
        return View(strategy);
    }
    
    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _strategyRepository.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    public async Task<IActionResult> ToggleEnabled(string id, CancellationToken cancellationToken)
    {
        var strategy = await _strategyRepository.GetByIdAsync(id, cancellationToken);
        if (strategy == null)
        {
            return NotFound();
        }
        
        strategy.Enabled = !strategy.Enabled;
        strategy.UpdatedAt = DateTime.UtcNow;
        await _strategyRepository.UpdateAsync(strategy, cancellationToken);
        
        return RedirectToAction(nameof(Index));
    }
}
