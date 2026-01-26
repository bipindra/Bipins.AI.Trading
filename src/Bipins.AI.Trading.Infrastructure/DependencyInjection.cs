using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.TickData;
using Bipins.AI.Trading.Infrastructure.Broker;
using Bipins.AI.Trading.Infrastructure.Broker.Alpaca;
using Bipins.AI.Trading.Infrastructure.Cache;
using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Infrastructure.TickData;
using Bipins.AI.Trading.Infrastructure.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Trading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register repositories
        services.AddScoped<ICandleRepository, Persistence.Repositories.CandleRepository>();
        services.AddScoped<ITickRepository, Persistence.Repositories.TickRepository>();
        services.AddScoped<IOrderRepository, Persistence.Repositories.OrderRepository>();
        services.AddScoped<ITradeDecisionRepository, Persistence.Repositories.TradeDecisionRepository>();
        services.AddScoped<IFillRepository, Persistence.Repositories.FillRepository>();
        services.AddScoped<IStrategyRepository, Persistence.Repositories.StrategyRepository>();
        
        // Register configuration services
        services.AddScoped<Application.Configuration.IConfigurationService, Configuration.ConfigurationService>();
        services.AddSingleton<Application.Configuration.IAlpacaCredentialsProvider, Configuration.AlpacaCredentialsProvider>();
        
        // Register resilience services
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.AddSingleton<Resilience.IResilienceService, Resilience.ResilienceService>();
        
        // Register cache
        services.AddMemoryCache();
        services.AddSingleton<ICacheStore, MemoryCacheStore>();
        
        // Register broker clients
        var brokerOptions = configuration.GetSection(BrokerOptions.SectionName).Get<BrokerOptions>();
        if (brokerOptions?.Provider == "Alpaca")
        {
            services.AddSingleton<IBrokerClient, AlpacaBrokerClient>();
            services.AddSingleton<IMarketDataClient, AlpacaMarketDataClient>();
            services.AddSingleton<ITickDataProvider, AlpacaTickDataProvider>();
        }
        else
        {
            // Fallback to no-op implementations if broker not configured
            services.AddSingleton<IBrokerClient, NoOpBrokerClient>();
            services.AddSingleton<IMarketDataClient, NoOpMarketDataClient>();
            services.AddSingleton<ITickDataProvider, NoOpTickDataProvider>();
        }
        
        // Register vector store from Bipins.AI NuGet package
        var vectorOptions = configuration.GetSection(Application.Options.VectorDbOptions.SectionName).Get<Application.Options.VectorDbOptions>();
        
        if (vectorOptions?.Provider == "Qdrant")
        {
            // Create Bipins.AI vector store provider (from NuGet package)
            services.AddSingleton(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                try
                {
                    return Vector.BipinsAIVectorProviderAdapter.CreateProvider(vectorOptions, loggerFactory);
                }
                catch (Exception ex)
                {
                    var logger = loggerFactory.CreateLogger(typeof(object));
                    logger.LogWarning(ex, "Failed to create Qdrant vector store from Bipins.AI package: {Error}. Using no-op implementation.", ex.Message);
                    return new NoOpVectorStore();
                }
            });
            
            // Adapter to bridge Bipins.AI vector store to Application.Ports.IVectorMemoryStore
            services.AddSingleton<IVectorMemoryStore>(sp =>
            {
                try
                {
                    var bipinsVectorStore = sp.GetRequiredService<object>();
                    return Vector.BipinsAIVectorAdapter.CreateAdapter(bipinsVectorStore);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetRequiredService<ILogger<IVectorMemoryStore>>();
                    logger.LogWarning(ex, "Failed to create vector store adapter. Using no-op implementation.");
                    return new NoOpVectorStore();
                }
            });
        }
        else
        {
            // Fallback to no-op implementation if vector store not configured
            services.AddSingleton<IVectorMemoryStore, NoOpVectorStore>();
        }
        
        // Register LLM providers from Bipins.AI NuGet package
        var llmOptions = new Application.Options.LLMOptions();
        configuration.GetSection(Application.Options.LLMOptions.SectionName).Bind(llmOptions);
        
        // Create Bipins.AI provider (used by chat service)
        services.AddSingleton<Bipins.AI.LLM.ILLMProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return LLM.BipinsAIProviderAdapter.CreateProvider(llmOptions, loggerFactory);
        });
        
        // Create Bipins.AI chat service (uses provider internally, recommended for chat operations)
        services.AddSingleton<Bipins.AI.LLM.IChatService>(sp =>
        {
            var provider = sp.GetRequiredService<Bipins.AI.LLM.ILLMProvider>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var options = new Bipins.AI.LLM.ChatServiceOptions
            {
                Model = llmOptions.Provider switch
                {
                    "OpenAI" => llmOptions.OpenAI.Model,
                    "Anthropic" => llmOptions.Anthropic.Model,
                    "AzureOpenAI" => llmOptions.AzureOpenAI.DeploymentName,
                    _ => "gpt-4"
                },
                Temperature = llmOptions.Provider switch
                {
                    "OpenAI" => llmOptions.OpenAI.Temperature,
                    "Anthropic" => llmOptions.Anthropic.Temperature,
                    "AzureOpenAI" => llmOptions.AzureOpenAI.Temperature,
                    _ => 0.7
                },
                MaxTokens = llmOptions.Provider switch
                {
                    "OpenAI" => llmOptions.OpenAI.MaxTokens,
                    "Anthropic" => llmOptions.Anthropic.MaxTokens,
                    "AzureOpenAI" => llmOptions.AzureOpenAI.MaxTokens,
                    _ => 2000
                },
                EmbeddingModel = llmOptions.Provider switch
                {
                    "OpenAI" => llmOptions.OpenAI.EmbeddingModel ?? "text-embedding-3-small",
                    "AzureOpenAI" => llmOptions.AzureOpenAI.EmbeddingDeploymentName ?? "text-embedding-3-small",
                    _ => "text-embedding-3-small"
                }
            };
            return new Bipins.AI.LLM.ChatService(provider, options, loggerFactory.CreateLogger<Bipins.AI.LLM.ChatService>());
        });
        
        // Register Application.LLM.ILLMService using Bipins.AI chat service via adapter
        services.AddScoped<Application.LLM.ILLMService>(sp =>
        {
            var bipinsChatService = sp.GetRequiredService<Bipins.AI.LLM.IChatService>();
            return new LLM.BipinsAIChatServiceAdapter(bipinsChatService);
        });
        
        // Register consumers
        services.AddScoped<FeaturesComputedConsumer>();
        
        return services;
    }
}

// No-op implementation for when Qdrant is not available (Application layer)
internal class NoOpVectorStore : IVectorMemoryStore
{
    public Task UpsertAsync(string collection, VectorDocument document, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    
    public Task<List<VectorSearchResult>> SearchAsync(string collection, float[] vector, int topK = 10, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<VectorSearchResult>());
    
    public Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
    
    public Task CreateCollectionAsync(string collection, int vectorSize, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

