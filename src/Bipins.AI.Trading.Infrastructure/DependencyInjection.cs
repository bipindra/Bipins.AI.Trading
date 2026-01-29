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
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Vectors.Qdrant;
using Bipins.AI.Vectors.Pinecone;
using Bipins.AI.Vectors.Milvus;
using Bipins.AI.Vectors.Weaviate;

namespace Bipins.AI.Trading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

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
        
        // Register vector store from Bipins.AI NuGet package using IBipinsAIBuilder
        var vectorOptions = configuration.GetSection(Application.Options.VectorDbOptions.SectionName).Get<Application.Options.VectorDbOptions>();
        RegisterVectorStore(services, vectorOptions);
        
        // Register Bipins.AI LLM provider + chat service (from NuGet package)
        var llmOptions = new Application.Options.LLMOptions();
        configuration.GetSection(Application.Options.LLMOptions.SectionName).Bind(llmOptions);

        services.AddSingleton<Bipins.AI.Providers.ILLMProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return llmOptions.Provider switch
            {
                "Anthropic" => CreateAnthropicProvider(llmOptions, httpClientFactory, loggerFactory),
                "AzureOpenAI" => CreateAzureOpenAiProvider(llmOptions, httpClientFactory, loggerFactory),
                _ => CreateOpenAiProvider(llmOptions, httpClientFactory, loggerFactory)
            };
        });

        services.AddSingleton<Bipins.AI.LLM.IChatService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var provider = sp.GetRequiredService<Bipins.AI.Providers.ILLMProvider>();
            
            // Configure ChatServiceOptions directly from app LLMOptions
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

            return new Bipins.AI.LLM.ChatService(
                provider,
                options,
                loggerFactory.CreateLogger<Bipins.AI.LLM.ChatService>());
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

    private static Bipins.AI.Providers.ILLMProvider CreateOpenAiProvider(
        LLMOptions llmOptions,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var options = new Bipins.AI.Providers.OpenAI.OpenAiOptions
        {
            ApiKey = llmOptions.OpenAI.ApiKey,
            DefaultChatModelId = llmOptions.OpenAI.Model,
            DefaultEmbeddingModelId = llmOptions.OpenAI.EmbeddingModel ?? "text-embedding-3-small"
        };
        var iOptions = Options.Create(options);

        var chatModel = new Bipins.AI.Providers.OpenAI.OpenAiChatModel(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.OpenAI.OpenAiChatModel>());

        var chatModelStreaming = new Bipins.AI.Providers.OpenAI.OpenAiChatModelStreaming(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.OpenAI.OpenAiChatModelStreaming>());

        var embeddingModel = new Bipins.AI.Providers.OpenAI.OpenAiEmbeddingModel(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.OpenAI.OpenAiEmbeddingModel>());

        return new Bipins.AI.Providers.OpenAI.OpenAiLLMProvider(
            chatModel,
            chatModelStreaming,
            embeddingModel,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.OpenAI.OpenAiLLMProvider>());
    }

    private static Bipins.AI.Providers.ILLMProvider CreateAnthropicProvider(
        LLMOptions llmOptions,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var options = new Bipins.AI.Providers.Anthropic.AnthropicOptions
        {
            ApiKey = llmOptions.Anthropic.ApiKey,
            DefaultChatModelId = llmOptions.Anthropic.Model
        };
        var iOptions = Options.Create(options);

        var chatModel = new Bipins.AI.Providers.Anthropic.AnthropicChatModel(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.Anthropic.AnthropicChatModel>());

        var chatModelStreaming = new Bipins.AI.Providers.Anthropic.AnthropicChatModelStreaming(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.Anthropic.AnthropicChatModelStreaming>());

        return new Bipins.AI.Providers.Anthropic.AnthropicLLMProvider(
            chatModel,
            chatModelStreaming,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.Anthropic.AnthropicLLMProvider>());
    }

    private static Bipins.AI.Providers.ILLMProvider CreateAzureOpenAiProvider(
        LLMOptions llmOptions,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var options = new Bipins.AI.Providers.AzureOpenAI.AzureOpenAiOptions
        {
            Endpoint = llmOptions.AzureOpenAI.Endpoint,
            ApiKey = llmOptions.AzureOpenAI.ApiKey,
            DefaultChatDeploymentName = llmOptions.AzureOpenAI.DeploymentName,
            DefaultEmbeddingDeploymentName = llmOptions.AzureOpenAI.EmbeddingDeploymentName ?? "text-embedding-3-small"
        };
        var iOptions = Options.Create(options);

        var chatModel = new Bipins.AI.Providers.AzureOpenAI.AzureOpenAiChatModel(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.AzureOpenAI.AzureOpenAiChatModel>());

        var chatModelStreaming = new Bipins.AI.Providers.AzureOpenAI.AzureOpenAiChatModelStreaming(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.AzureOpenAI.AzureOpenAiChatModelStreaming>());

        var embeddingModel = new Bipins.AI.Providers.AzureOpenAI.AzureOpenAiEmbeddingModel(
            httpClientFactory,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.AzureOpenAI.AzureOpenAiEmbeddingModel>());

        return new Bipins.AI.Providers.AzureOpenAI.AzureOpenAiLLMProvider(
            chatModel,
            chatModelStreaming,
            embeddingModel,
            iOptions,
            loggerFactory.CreateLogger<Bipins.AI.Providers.AzureOpenAI.AzureOpenAiLLMProvider>());
    }

    private static void RegisterVectorStore(IServiceCollection services, Application.Options.VectorDbOptions? vectorOptions)
    {
        if (vectorOptions == null || string.IsNullOrEmpty(vectorOptions.Provider))
        {
            // Fallback to no-op implementation if vector store not configured
            services.AddSingleton<IVectorMemoryStore, NoOpVectorStore>();
            return;
        }

        // Register Bipins.AI services and vector stores via IBipinsAIBuilder
        // AddBipinsAI returns IBipinsAIBuilder which can be chained with provider-specific Add methods
        var builder = services.AddBipinsAI(_ => { });

        switch (vectorOptions.Provider)
        {
            case "Qdrant":
                builder.AddQdrant(options =>
                {
                    options.Endpoint = vectorOptions.Qdrant?.Endpoint ?? "http://localhost:6333";
                    options.DefaultCollectionName = vectorOptions.Qdrant?.CollectionName ?? "trading_decisions";
                    options.CreateCollectionIfMissing = true;
                });
                break;

            case "Pinecone":
                builder.AddPinecone(options =>
                {
                    // Configure Pinecone options
                    // Note: Property names may need adjustment based on actual Bipins.AI API
                    if (vectorOptions.Pinecone != null && !string.IsNullOrEmpty(vectorOptions.Pinecone.ApiKey))
                    {
                        options.ApiKey = vectorOptions.Pinecone.ApiKey;
                    }
                    if (vectorOptions.Pinecone != null && !string.IsNullOrEmpty(vectorOptions.Pinecone.Environment))
                    {
                        options.Environment = vectorOptions.Pinecone.Environment;
                    }
                });
                break;

            case "Milvus":
                builder.AddMilvus(options =>
                {
                    // Configure Milvus options
                    // Note: Property names may need adjustment based on actual Bipins.AI API
                    // For now, using basic configuration - adjust as needed
                });
                break;

            case "Weaviate":
                builder.AddWeaviate(options =>
                {
                    // Configure Weaviate options
                    if (vectorOptions.Weaviate != null)
                    {
                        options.Endpoint = vectorOptions.Weaviate.Endpoint ?? "http://localhost:8080";
                        if (!string.IsNullOrEmpty(vectorOptions.Weaviate.ApiKey))
                        {
                            options.ApiKey = vectorOptions.Weaviate.ApiKey;
                        }
                    }
                });
                break;

            default:
                // Fallback to no-op implementation for unsupported providers
                services.AddSingleton<IVectorMemoryStore, NoOpVectorStore>();
                return;
        }

        // Adapter to bridge Bipins.AI vector store to Application.Ports.IVectorMemoryStore
        services.AddSingleton<IVectorMemoryStore>(sp =>
        {
            try
            {
                var bipinsVectorStore = sp.GetRequiredService<Bipins.AI.Vector.IVectorStore>();
                return Vector.VectorStoreAdapter.CreateAdapter(bipinsVectorStore);
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<IVectorMemoryStore>>();
                logger.LogWarning(ex, "Failed to create vector store adapter. Using no-op implementation.");
                return new NoOpVectorStore();
            }
        });
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

