using System.ComponentModel.DataAnnotations;
using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly IConfigurationService _configurationService;
    private readonly IAlpacaCredentialsProvider _credentialsProvider;
    private readonly ILogger<SettingsController> _logger;
    
    public SettingsController(
        IConfigurationService configurationService,
        IAlpacaCredentialsProvider credentialsProvider,
        ILogger<SettingsController> logger)
    {
        _configurationService = configurationService;
        _credentialsProvider = credentialsProvider;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var credentials = await _configurationService.GetAlpacaCredentialsAsync(cancellationToken);
        var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
        var vectorDbOptions = await _configurationService.GetVectorDbOptionsAsync(cancellationToken);
        
        var viewModel = new SettingsViewModel
        {
            Alpaca = new AlpacaSettingsViewModel
            {
                ApiKey = credentials?.ApiKey ?? string.Empty,
                HasApiKey = credentials != null && !string.IsNullOrEmpty(credentials.ApiKey),
                BaseUrl = "https://paper-api.alpaca.markets"
            },
            LLM = llmOptions,
            VectorDb = vectorDbOptions
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlpacaCredentials(AlpacaSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            var vectorDbOptions = await _configurationService.GetVectorDbOptionsAsync(cancellationToken);
            var viewModel = new SettingsViewModel
            {
                Alpaca = model,
                LLM = llmOptions,
                VectorDb = vectorDbOptions
            };
            return View("Index", viewModel);
        }
        
        try
        {
            await _configurationService.SetAlpacaCredentialsAsync(model.ApiKey, model.ApiSecret, cancellationToken);
            _credentialsProvider.InvalidateCache();
            
            TempData["SuccessMessage"] = "Alpaca API credentials have been saved successfully.";
            _logger.LogInformation("Alpaca API credentials updated via UI");
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Alpaca credentials");
            ModelState.AddModelError("", "An error occurred while saving the credentials. Please try again.");
            var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            var vectorDbOptions = await _configurationService.GetVectorDbOptionsAsync(cancellationToken);
            var viewModel = new SettingsViewModel
            {
                Alpaca = model,
                LLM = llmOptions,
                VectorDb = vectorDbOptions
            };
            return View("Index", viewModel);
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLLMProvider(string provider, CancellationToken cancellationToken)
    {
        try
        {
            var currentOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            currentOptions.Provider = provider;
            await _configurationService.SetLLMOptionsAsync(currentOptions, cancellationToken);
            
            TempData["SuccessMessage"] = $"LLM provider set to {provider}. Restart the application for changes to take effect.";
            _logger.LogInformation("LLM provider updated to {Provider} via UI", provider);
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update LLM provider");
            TempData["ErrorMessage"] = "An error occurred while saving the provider selection. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLLMSettings([Bind(Prefix = "LLM")] LLMOptions model, CancellationToken cancellationToken)
    {
        try
        {
            // Get current options to preserve provider if not set in model
            var currentOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            
            // Ensure nested objects are initialized
            if (model.OpenAI == null)
            {
                model.OpenAI = new Application.Options.OpenAIOptions();
            }
            if (model.Anthropic == null)
            {
                model.Anthropic = new Application.Options.AnthropicOptions();
            }
            if (model.AzureOpenAI == null)
            {
                model.AzureOpenAI = new Application.Options.AzureOpenAIOptions();
            }
            
            // If nested properties are empty, try to get them from form directly
            var provider = Request.Form["LLM.Provider"].ToString();
            if (!string.IsNullOrEmpty(provider))
            {
                model.Provider = provider;
            }
            
            // Read provider-specific settings from form if model binding failed
            if (model.Provider == "OpenAI")
            {
                var apiKey = Request.Form["LLM.OpenAI.ApiKey"].ToString();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    model.OpenAI.ApiKey = apiKey;
                }
                var modelName = Request.Form["LLM.OpenAI.Model"].ToString();
                if (!string.IsNullOrEmpty(modelName))
                {
                    model.OpenAI.Model = modelName;
                }
                var temperature = Request.Form["LLM.OpenAI.Temperature"].ToString();
                if (!string.IsNullOrEmpty(temperature) && double.TryParse(temperature, out var temp))
                {
                    model.OpenAI.Temperature = temp;
                }
                var maxTokens = Request.Form["LLM.OpenAI.MaxTokens"].ToString();
                if (!string.IsNullOrEmpty(maxTokens) && int.TryParse(maxTokens, out var tokens))
                {
                    model.OpenAI.MaxTokens = tokens;
                }
                var embeddingModel = Request.Form["LLM.OpenAI.EmbeddingModel"].ToString();
                if (!string.IsNullOrEmpty(embeddingModel))
                {
                    model.OpenAI.EmbeddingModel = embeddingModel;
                }
                
                currentOptions.OpenAI = model.OpenAI;
                currentOptions.Provider = "OpenAI";
            }
            else if (model.Provider == "Anthropic")
            {
                var apiKey = Request.Form["LLM.Anthropic.ApiKey"].ToString();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    model.Anthropic.ApiKey = apiKey;
                }
                var modelName = Request.Form["LLM.Anthropic.Model"].ToString();
                if (!string.IsNullOrEmpty(modelName))
                {
                    model.Anthropic.Model = modelName;
                }
                var temperature = Request.Form["LLM.Anthropic.Temperature"].ToString();
                if (!string.IsNullOrEmpty(temperature) && double.TryParse(temperature, out var temp))
                {
                    model.Anthropic.Temperature = temp;
                }
                var maxTokens = Request.Form["LLM.Anthropic.MaxTokens"].ToString();
                if (!string.IsNullOrEmpty(maxTokens) && int.TryParse(maxTokens, out var tokens))
                {
                    model.Anthropic.MaxTokens = tokens;
                }
                
                currentOptions.Anthropic = model.Anthropic;
                currentOptions.Provider = "Anthropic";
            }
            else if (model.Provider == "AzureOpenAI")
            {
                var endpoint = Request.Form["LLM.AzureOpenAI.Endpoint"].ToString();
                if (!string.IsNullOrEmpty(endpoint))
                {
                    model.AzureOpenAI.Endpoint = endpoint;
                }
                var apiKey = Request.Form["LLM.AzureOpenAI.ApiKey"].ToString();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    model.AzureOpenAI.ApiKey = apiKey;
                }
                var deploymentName = Request.Form["LLM.AzureOpenAI.DeploymentName"].ToString();
                if (!string.IsNullOrEmpty(deploymentName))
                {
                    model.AzureOpenAI.DeploymentName = deploymentName;
                }
                var temperature = Request.Form["LLM.AzureOpenAI.Temperature"].ToString();
                if (!string.IsNullOrEmpty(temperature) && double.TryParse(temperature, out var temp))
                {
                    model.AzureOpenAI.Temperature = temp;
                }
                var maxTokens = Request.Form["LLM.AzureOpenAI.MaxTokens"].ToString();
                if (!string.IsNullOrEmpty(maxTokens) && int.TryParse(maxTokens, out var tokens))
                {
                    model.AzureOpenAI.MaxTokens = tokens;
                }
                var embeddingDeployment = Request.Form["LLM.AzureOpenAI.EmbeddingDeploymentName"].ToString();
                if (!string.IsNullOrEmpty(embeddingDeployment))
                {
                    model.AzureOpenAI.EmbeddingDeploymentName = embeddingDeployment;
                }
                
                currentOptions.AzureOpenAI = model.AzureOpenAI;
                currentOptions.Provider = "AzureOpenAI";
            }
            
            _logger.LogInformation("Saving LLM settings for provider: {Provider}", currentOptions.Provider);
            
            await _configurationService.SetLLMOptionsAsync(currentOptions, cancellationToken);
            TempData["SuccessMessage"] = $"{currentOptions.Provider} settings have been saved successfully. Restart the application for changes to take effect.";
            _logger.LogInformation("{Provider} settings updated via UI", currentOptions.Provider);
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update LLM settings");
            TempData["ErrorMessage"] = "An error occurred while saving the settings. Please try again.";
            
            var credentials = await _configurationService.GetAlpacaCredentialsAsync(cancellationToken);
            var vectorDbOptions = await _configurationService.GetVectorDbOptionsAsync(cancellationToken);
            var viewModel = new SettingsViewModel
            {
                Alpaca = new AlpacaSettingsViewModel
                {
                    ApiKey = credentials?.ApiKey ?? string.Empty,
                    HasApiKey = credentials != null && !string.IsNullOrEmpty(credentials.ApiKey),
                    BaseUrl = "https://paper-api.alpaca.markets"
                },
                LLM = model,
                VectorDb = vectorDbOptions
            };
            return View("Index", viewModel);
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVectorDbSettings([Bind(Prefix = "VectorDb")] VectorDbOptions model, CancellationToken cancellationToken)
    {
        // Ensure Qdrant is initialized if null (model binding might not create nested objects)
        if (model.Qdrant == null)
        {
            model.Qdrant = new Application.Options.QdrantOptions();
        }
        
        // Get current options to preserve provider if not set
        var currentOptions = await _configurationService.GetVectorDbOptionsAsync(cancellationToken);
        if (string.IsNullOrEmpty(model.Provider))
        {
            model.Provider = currentOptions.Provider;
        }
        
        // If Qdrant properties are empty, try to get them from form directly
        var endpoint = Request.Form["VectorDb.Qdrant.Endpoint"].ToString();
        var collectionName = Request.Form["VectorDb.Qdrant.CollectionName"].ToString();
        
        if (!string.IsNullOrEmpty(endpoint))
        {
            model.Qdrant.Endpoint = endpoint;
        }
        if (!string.IsNullOrEmpty(collectionName))
        {
            model.Qdrant.CollectionName = collectionName;
        }
        
        if (!ModelState.IsValid)
        {
            var credentials = await _configurationService.GetAlpacaCredentialsAsync(cancellationToken);
            var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            var viewModel = new SettingsViewModel
            {
                Alpaca = new AlpacaSettingsViewModel
                {
                    ApiKey = credentials?.ApiKey ?? string.Empty,
                    HasApiKey = credentials != null && !string.IsNullOrEmpty(credentials.ApiKey),
                    BaseUrl = "https://paper-api.alpaca.markets"
                },
                LLM = llmOptions,
                VectorDb = model
            };
            return View("Index", viewModel);
        }
        
        try
        {
            _logger.LogInformation("Saving Vector DB settings: Endpoint={Endpoint}, CollectionName={CollectionName}", 
                model.Qdrant.Endpoint, model.Qdrant.CollectionName);
            
            await _configurationService.SetVectorDbOptionsAsync(model, cancellationToken);
            TempData["SuccessMessage"] = "Vector DB settings have been saved successfully. Restart the application for changes to take effect.";
            _logger.LogInformation("Vector DB settings updated via UI: Endpoint={Endpoint}", model.Qdrant.Endpoint);
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Vector DB settings");
            TempData["ErrorMessage"] = $"An error occurred while saving the settings: {ex.Message}";
            var credentials = await _configurationService.GetAlpacaCredentialsAsync(cancellationToken);
            var llmOptions = await _configurationService.GetLLMOptionsAsync(cancellationToken);
            var viewModel = new SettingsViewModel
            {
                Alpaca = new AlpacaSettingsViewModel
                {
                    ApiKey = credentials?.ApiKey ?? string.Empty,
                    HasApiKey = credentials != null && !string.IsNullOrEmpty(credentials.ApiKey),
                    BaseUrl = "https://paper-api.alpaca.markets"
                },
                LLM = llmOptions,
                VectorDb = model
            };
            return View("Index", viewModel);
        }
    }
}

public class SettingsViewModel
{
    public AlpacaSettingsViewModel Alpaca { get; set; } = new();
    public LLMOptions LLM { get; set; } = new();
    public VectorDbOptions VectorDb { get; set; } = new();
}

public class AlpacaSettingsViewModel
{
    [Required(ErrorMessage = "API Key is required")]
    public string ApiKey { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "API Secret is required")]
    public string ApiSecret { get; set; } = string.Empty;
    
    public bool HasApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://paper-api.alpaca.markets";
}
