using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Infrastructure.Persistence;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly TradingDbContext _dbContext;
    private readonly ILogger<ConfigurationService> _logger;
    
    public ConfigurationService(TradingDbContext dbContext, ILogger<ConfigurationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.Set<ConfigurationEntity>()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        return config?.Value;
    }
    
    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.Set<ConfigurationEntity>()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        
        if (config == null)
        {
            config = new ConfigurationEntity
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Set<ConfigurationEntity>().Add(config);
        }
        else
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<bool> HasValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ConfigurationEntity>()
            .AnyAsync(c => c.Key == key && !string.IsNullOrEmpty(c.Value), cancellationToken);
    }
    
    public async Task<AlpacaCredentials?> GetAlpacaCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = await GetValueAsync("Alpaca:ApiKey", cancellationToken);
        var apiSecret = await GetValueAsync("Alpaca:ApiSecret", cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            return null;
        }
        
        return new AlpacaCredentials
        {
            ApiKey = apiKey,
            ApiSecret = apiSecret
        };
    }
    
    public async Task SetAlpacaCredentialsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        await SetValueAsync("Alpaca:ApiKey", apiKey, cancellationToken);
        await SetValueAsync("Alpaca:ApiSecret", apiSecret, cancellationToken);
        _logger.LogInformation("Alpaca API credentials updated");
    }
    
    public async Task<LLMOptions> GetLLMOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = new LLMOptions();
        
        options.Provider = await GetValueAsync("LLM:Provider", cancellationToken) ?? "OpenAI";
        options.OpenAI.ApiKey = await GetValueAsync("LLM:OpenAI:ApiKey", cancellationToken) ?? string.Empty;
        options.OpenAI.Model = await GetValueAsync("LLM:OpenAI:Model", cancellationToken) ?? "gpt-4";
        options.OpenAI.Temperature = double.TryParse(await GetValueAsync("LLM:OpenAI:Temperature", cancellationToken), out var openAITemp) ? openAITemp : 0.7;
        options.OpenAI.MaxTokens = int.TryParse(await GetValueAsync("LLM:OpenAI:MaxTokens", cancellationToken), out var openAIMaxTokens) ? openAIMaxTokens : 2000;
        options.OpenAI.EmbeddingModel = await GetValueAsync("LLM:OpenAI:EmbeddingModel", cancellationToken) ?? "text-embedding-3-small";
        
        options.Anthropic.ApiKey = await GetValueAsync("LLM:Anthropic:ApiKey", cancellationToken) ?? string.Empty;
        options.Anthropic.Model = await GetValueAsync("LLM:Anthropic:Model", cancellationToken) ?? "claude-3-opus-20240229";
        options.Anthropic.Temperature = double.TryParse(await GetValueAsync("LLM:Anthropic:Temperature", cancellationToken), out var anthropicTemp) ? anthropicTemp : 0.7;
        options.Anthropic.MaxTokens = int.TryParse(await GetValueAsync("LLM:Anthropic:MaxTokens", cancellationToken), out var anthropicMaxTokens) ? anthropicMaxTokens : 2000;
        
        options.AzureOpenAI.Endpoint = await GetValueAsync("LLM:AzureOpenAI:Endpoint", cancellationToken) ?? string.Empty;
        options.AzureOpenAI.ApiKey = await GetValueAsync("LLM:AzureOpenAI:ApiKey", cancellationToken) ?? string.Empty;
        options.AzureOpenAI.DeploymentName = await GetValueAsync("LLM:AzureOpenAI:DeploymentName", cancellationToken) ?? "gpt-4";
        options.AzureOpenAI.Temperature = double.TryParse(await GetValueAsync("LLM:AzureOpenAI:Temperature", cancellationToken), out var azureTemp) ? azureTemp : 0.7;
        options.AzureOpenAI.MaxTokens = int.TryParse(await GetValueAsync("LLM:AzureOpenAI:MaxTokens", cancellationToken), out var azureMaxTokens) ? azureMaxTokens : 2000;
        options.AzureOpenAI.EmbeddingDeploymentName = await GetValueAsync("LLM:AzureOpenAI:EmbeddingDeploymentName", cancellationToken) ?? "text-embedding-3-small";
        
        return options;
    }
    
    public async Task SetLLMOptionsAsync(LLMOptions options, CancellationToken cancellationToken = default)
    {
        await SetValueAsync("LLM:Provider", options.Provider, cancellationToken);
        
        await SetValueAsync("LLM:OpenAI:ApiKey", options.OpenAI.ApiKey, cancellationToken);
        await SetValueAsync("LLM:OpenAI:Model", options.OpenAI.Model, cancellationToken);
        await SetValueAsync("LLM:OpenAI:Temperature", options.OpenAI.Temperature.ToString(), cancellationToken);
        await SetValueAsync("LLM:OpenAI:MaxTokens", options.OpenAI.MaxTokens.ToString(), cancellationToken);
        await SetValueAsync("LLM:OpenAI:EmbeddingModel", options.OpenAI.EmbeddingModel ?? "text-embedding-3-small", cancellationToken);
        
        await SetValueAsync("LLM:Anthropic:ApiKey", options.Anthropic.ApiKey, cancellationToken);
        await SetValueAsync("LLM:Anthropic:Model", options.Anthropic.Model, cancellationToken);
        await SetValueAsync("LLM:Anthropic:Temperature", options.Anthropic.Temperature.ToString(), cancellationToken);
        await SetValueAsync("LLM:Anthropic:MaxTokens", options.Anthropic.MaxTokens.ToString(), cancellationToken);
        
        await SetValueAsync("LLM:AzureOpenAI:Endpoint", options.AzureOpenAI.Endpoint, cancellationToken);
        await SetValueAsync("LLM:AzureOpenAI:ApiKey", options.AzureOpenAI.ApiKey, cancellationToken);
        await SetValueAsync("LLM:AzureOpenAI:DeploymentName", options.AzureOpenAI.DeploymentName, cancellationToken);
        await SetValueAsync("LLM:AzureOpenAI:Temperature", options.AzureOpenAI.Temperature.ToString(), cancellationToken);
        await SetValueAsync("LLM:AzureOpenAI:MaxTokens", options.AzureOpenAI.MaxTokens.ToString(), cancellationToken);
        await SetValueAsync("LLM:AzureOpenAI:EmbeddingDeploymentName", options.AzureOpenAI.EmbeddingDeploymentName ?? "text-embedding-3-small", cancellationToken);
        
        _logger.LogInformation("LLM options updated");
    }
    
    public async Task<VectorDbOptions> GetVectorDbOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = new VectorDbOptions();
        
        options.Provider = await GetValueAsync("VectorDb:Provider", cancellationToken) ?? "Qdrant";
        
        // Qdrant options
        options.Qdrant.Endpoint = await GetValueAsync("VectorDb:Qdrant:Endpoint", cancellationToken) ?? "http://localhost:6333";
        options.Qdrant.CollectionName = await GetValueAsync("VectorDb:Qdrant:CollectionName", cancellationToken) ?? "trading_decisions";
        
        // Pinecone options
        options.Pinecone.ApiKey = await GetValueAsync("VectorDb:Pinecone:ApiKey", cancellationToken) ?? string.Empty;
        options.Pinecone.Environment = await GetValueAsync("VectorDb:Pinecone:Environment", cancellationToken) ?? string.Empty;
        options.Pinecone.IndexName = await GetValueAsync("VectorDb:Pinecone:IndexName", cancellationToken) ?? "trading-decisions";
        
        // Milvus options
        options.Milvus.Host = await GetValueAsync("VectorDb:Milvus:Host", cancellationToken) ?? "localhost";
        var milvusPort = await GetValueAsync("VectorDb:Milvus:Port", cancellationToken);
        options.Milvus.Port = int.TryParse(milvusPort, out var port) ? port : 19530;
        options.Milvus.CollectionName = await GetValueAsync("VectorDb:Milvus:CollectionName", cancellationToken) ?? "trading_decisions";
        
        // Weaviate options
        options.Weaviate.Endpoint = await GetValueAsync("VectorDb:Weaviate:Endpoint", cancellationToken) ?? "http://localhost:8080";
        options.Weaviate.ApiKey = await GetValueAsync("VectorDb:Weaviate:ApiKey", cancellationToken) ?? string.Empty;
        options.Weaviate.ClassName = await GetValueAsync("VectorDb:Weaviate:ClassName", cancellationToken) ?? "TradingDecision";
        
        return options;
    }
    
    public async Task SetVectorDbOptionsAsync(VectorDbOptions options, CancellationToken cancellationToken = default)
    {
        await SetValueAsync("VectorDb:Provider", options.Provider, cancellationToken);
        
        // Qdrant options
        await SetValueAsync("VectorDb:Qdrant:Endpoint", options.Qdrant.Endpoint, cancellationToken);
        await SetValueAsync("VectorDb:Qdrant:CollectionName", options.Qdrant.CollectionName, cancellationToken);
        
        // Pinecone options
        await SetValueAsync("VectorDb:Pinecone:ApiKey", options.Pinecone.ApiKey, cancellationToken);
        await SetValueAsync("VectorDb:Pinecone:Environment", options.Pinecone.Environment, cancellationToken);
        await SetValueAsync("VectorDb:Pinecone:IndexName", options.Pinecone.IndexName, cancellationToken);
        
        // Milvus options
        await SetValueAsync("VectorDb:Milvus:Host", options.Milvus.Host, cancellationToken);
        await SetValueAsync("VectorDb:Milvus:Port", options.Milvus.Port.ToString(), cancellationToken);
        await SetValueAsync("VectorDb:Milvus:CollectionName", options.Milvus.CollectionName, cancellationToken);
        
        // Weaviate options
        await SetValueAsync("VectorDb:Weaviate:Endpoint", options.Weaviate.Endpoint, cancellationToken);
        await SetValueAsync("VectorDb:Weaviate:ApiKey", options.Weaviate.ApiKey, cancellationToken);
        await SetValueAsync("VectorDb:Weaviate:ClassName", options.Weaviate.ClassName, cancellationToken);
        
        _logger.LogInformation("Vector DB options updated for provider: {Provider}", options.Provider);
    }
}
