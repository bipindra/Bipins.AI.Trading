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
    public async Task<IActionResult> Create(Strategy strategy, string? alertsJson, string? conditionsJson, string? finalAction, CancellationToken cancellationToken)
    {
        // Initialize collections if null
        strategy.Alerts ??= new List<IndicatorAlert>();
        strategy.Conditions ??= new List<AlertCondition>();
        
        // Parse alerts from JSON
        if (!string.IsNullOrEmpty(alertsJson))
        {
            try
            {
                var alertsData = System.Text.Json.JsonSerializer.Deserialize<List<AlertData>>(alertsJson);
                if (alertsData != null && alertsData.Count > 0)
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
                        Action = TradeAction.Hold, // Alerts don't have actions anymore, use Hold as default
                        Order = index
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing alerts JSON: {AlertsJson}", alertsJson);
                ModelState.AddModelError("", $"Error parsing alerts: {ex.Message}");
            }
        }
        
        // Parse conditions from JSON
        if (!string.IsNullOrEmpty(conditionsJson))
        {
            try
            {
                var conditionsData = System.Text.Json.JsonSerializer.Deserialize<List<ConditionData>>(conditionsJson);
                if (conditionsData != null && conditionsData.Count > 0)
                {
                    strategy.Conditions = conditionsData
                        .Where(c => !string.IsNullOrEmpty(c.LeftAlertId) && !string.IsNullOrEmpty(c.RightAlertId))
                        .Select((c, index) => new AlertCondition
                        {
                            Id = c.Id ?? Guid.NewGuid().ToString(),
                            StrategyId = strategy.Id,
                            LeftAlertId = c.LeftAlertId,
                            Operator = Enum.Parse<ConditionOperator>(c.Operator),
                            RightAlertId = c.RightAlertId,
                            Action = TradeAction.Hold, // Conditions don't have actions anymore, use Hold as default
                            Order = index
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing conditions JSON: {ConditionsJson}", conditionsJson);
                ModelState.AddModelError("", $"Error parsing conditions: {ex.Message}");
            }
        }
        
        // Validate that we have at least one alert
        if (strategy.Alerts.Count == 0)
        {
            ModelState.AddModelError("", "At least one alert criteria is required.");
        }
        
        // Validate final action
        if (string.IsNullOrEmpty(finalAction) || !Enum.TryParse<TradeAction>(finalAction, out var action))
        {
            ModelState.AddModelError("", "A strategy action (Buy or Sell) is required.");
        }
        else
        {
            strategy.FinalAction = action;
        }
        
        // Validate Timeframe
        if (strategy.Timeframe == null)
        {
            ModelState.AddModelError("Timeframe", "Timeframe is required.");
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                await _strategyRepository.AddAsync(strategy, cancellationToken);
                TempData["Message"] = "Strategy created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving strategy");
                ModelState.AddModelError("", $"Error saving strategy: {ex.Message}");
            }
        }
        
        // Log ModelState errors for debugging
        foreach (var error in ModelState)
        {
            foreach (var errorMessage in error.Value.Errors)
            {
                _logger.LogWarning("ModelState Error - {Key}: {Message}", error.Key, errorMessage.ErrorMessage);
            }
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
    public async Task<IActionResult> Edit(Strategy strategy, string? alertsJson, string? conditionsJson, string? finalAction, CancellationToken cancellationToken)
    {
        // Initialize collections if null
        strategy.Alerts ??= new List<IndicatorAlert>();
        strategy.Conditions ??= new List<AlertCondition>();
        
        // Parse alerts from JSON
        if (!string.IsNullOrEmpty(alertsJson))
        {
            try
            {
                var alertsData = System.Text.Json.JsonSerializer.Deserialize<List<AlertData>>(alertsJson);
                if (alertsData != null && alertsData.Count > 0)
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
                        Action = TradeAction.Hold, // Alerts don't have actions anymore, use Hold as default
                        Order = index
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing alerts JSON: {AlertsJson}", alertsJson);
                ModelState.AddModelError("", $"Error parsing alerts: {ex.Message}");
            }
        }
        
        // Parse conditions from JSON
        if (!string.IsNullOrEmpty(conditionsJson))
        {
            try
            {
                var conditionsData = System.Text.Json.JsonSerializer.Deserialize<List<ConditionData>>(conditionsJson);
                if (conditionsData != null && conditionsData.Count > 0)
                {
                    strategy.Conditions = conditionsData
                        .Where(c => !string.IsNullOrEmpty(c.LeftAlertId) && !string.IsNullOrEmpty(c.RightAlertId))
                        .Select((c, index) => new AlertCondition
                        {
                            Id = c.Id ?? Guid.NewGuid().ToString(),
                            StrategyId = strategy.Id,
                            LeftAlertId = c.LeftAlertId,
                            Operator = Enum.Parse<ConditionOperator>(c.Operator),
                            RightAlertId = c.RightAlertId,
                            Action = TradeAction.Hold, // Conditions don't have actions anymore, use Hold as default
                            Order = index
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing conditions JSON: {ConditionsJson}", conditionsJson);
                ModelState.AddModelError("", $"Error parsing conditions: {ex.Message}");
            }
        }
        
        // Validate that we have at least one alert
        if (strategy.Alerts.Count == 0)
        {
            ModelState.AddModelError("", "At least one alert criteria is required.");
        }
        
        // Validate final action
        if (string.IsNullOrEmpty(finalAction) || !Enum.TryParse<TradeAction>(finalAction, out var action))
        {
            ModelState.AddModelError("", "A strategy action (Buy or Sell) is required.");
        }
        else
        {
            strategy.FinalAction = action;
        }
        
        // Validate Timeframe
        if (strategy.Timeframe == null)
        {
            ModelState.AddModelError("Timeframe", "Timeframe is required.");
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                strategy.UpdatedAt = DateTime.UtcNow;
                await _strategyRepository.UpdateAsync(strategy, cancellationToken);
                TempData["Message"] = "Strategy updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strategy");
                ModelState.AddModelError("", $"Error updating strategy: {ex.Message}");
            }
        }
        
        // Log ModelState errors for debugging
        foreach (var error in ModelState)
        {
            foreach (var errorMessage in error.Value.Errors)
            {
                _logger.LogWarning("ModelState Error - {Key}: {Message}", error.Key, errorMessage.ErrorMessage);
            }
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
