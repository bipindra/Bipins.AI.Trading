using Bipins.AI.Trading.Application.Agents;
using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class AgentsController : Controller
{
    private readonly TradingAgent _tradingAgent;
    private readonly AgentMemory _agentMemory;
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IConfigurationService _configurationService;
    private readonly LLMOptions _llmOptions;
    private readonly ILogger<AgentsController> _logger;
    
    public AgentsController(
        TradingAgent tradingAgent,
        AgentMemory agentMemory,
        IVectorMemoryStore vectorStore,
        IConfigurationService configurationService,
        IOptions<LLMOptions> llmOptions,
        ILogger<AgentsController> logger)
    {
        _tradingAgent = tradingAgent;
        _agentMemory = agentMemory;
        _vectorStore = vectorStore;
        _configurationService = configurationService;
        _llmOptions = llmOptions.Value;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        // Get LLM options from database (which includes UI-saved settings)
        var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
        
        var model = new AgentStatusViewModel
        {
            Provider = llmOptions.Provider,
            IsConfigured = IsLLMConfigured(llmOptions),
            Model = GetCurrentModel(llmOptions)
        };
        
        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> Memory(CancellationToken cancellationToken)
    {
        try
        {
            var collectionExists = await _vectorStore.CollectionExistsAsync("trading_scenarios", cancellationToken);
            
            var model = new AgentMemoryViewModel
            {
                CollectionExists = collectionExists,
                CollectionName = "trading_scenarios"
            };
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agent memory");
            TempData["Error"] = "Failed to load agent memory: " + ex.Message;
            return View(new AgentMemoryViewModel { CollectionExists = false });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> TestDecision(string? symbol = null, string? timeframe = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(symbol))
            symbol = "SPY";
        if (string.IsNullOrEmpty(timeframe))
            timeframe = "5m";
        
        try
        {
            var context = await _tradingAgent.BuildContextAsync(
                new Symbol(symbol),
                new Timeframe(timeframe),
                cancellationToken);
            
            var decision = await _tradingAgent.MakeDecisionAsync(context, cancellationToken);
            
            var model = new AgentTestDecisionViewModel
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Context = context,
                Decision = decision,
                Timestamp = DateTime.UtcNow
            };
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing agent decision");
            TempData["Error"] = "Failed to test agent decision: " + ex.Message;
            return View(new AgentTestDecisionViewModel
            {
                Symbol = symbol ?? "SPY",
                Timeframe = timeframe ?? "5m"
            });
        }
    }
    
    private bool IsLLMConfigured(LLMOptions llmOptions)
    {
        return llmOptions.Provider switch
        {
            "OpenAI" => !string.IsNullOrEmpty(llmOptions.OpenAI.ApiKey),
            "Anthropic" => !string.IsNullOrEmpty(llmOptions.Anthropic.ApiKey),
            "AzureOpenAI" => !string.IsNullOrEmpty(llmOptions.AzureOpenAI.ApiKey) && 
                            !string.IsNullOrEmpty(llmOptions.AzureOpenAI.Endpoint),
            _ => false
        };
    }
    
    private string GetCurrentModel(LLMOptions llmOptions)
    {
        return llmOptions.Provider switch
        {
            "OpenAI" => llmOptions.OpenAI.Model,
            "Anthropic" => llmOptions.Anthropic.Model,
            "AzureOpenAI" => llmOptions.AzureOpenAI.DeploymentName,
            _ => "Not configured"
        };
    }
}

public class AgentStatusViewModel
{
    public string Provider { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public string Model { get; set; } = string.Empty;
}

public class AgentMemoryViewModel
{
    public bool CollectionExists { get; set; }
    public string CollectionName { get; set; } = string.Empty;
}

public class AgentTestDecisionViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public AgentContext? Context { get; set; }
    public Bipins.AI.Trading.Domain.Entities.TradeDecision? Decision { get; set; }
    public DateTime Timestamp { get; set; }
}
