// Adapter to convert Trading application options to Bipins.AI provider options
// and register Bipins.AI providers

using Bipins.AI.LLM.Providers;
using Bipins.AI.Trading.Application.Options;

namespace Bipins.AI.Trading.Infrastructure.LLM;

public static class BipinsAIProviderAdapter
{
    public static Bipins.AI.LLM.ILLMProvider CreateProvider(LLMOptions options, ILoggerFactory loggerFactory)
    {
        return options.Provider switch
        {
            "OpenAI" => new OpenAIProvider(
                new OpenAIProviderOptions
                {
                    ApiKey = options.OpenAI.ApiKey,
                    Model = options.OpenAI.Model,
                    Temperature = options.OpenAI.Temperature,
                    MaxTokens = options.OpenAI.MaxTokens,
                    EmbeddingModel = options.OpenAI.EmbeddingModel
                },
                loggerFactory.CreateLogger<OpenAIProvider>()),
            
            "Anthropic" => new AnthropicProvider(
                new AnthropicProviderOptions
                {
                    ApiKey = options.Anthropic.ApiKey,
                    Model = options.Anthropic.Model,
                    Temperature = options.Anthropic.Temperature,
                    MaxTokens = options.Anthropic.MaxTokens
                },
                loggerFactory.CreateLogger<AnthropicProvider>()),
            
            "AzureOpenAI" => new AzureOpenAIProvider(
                new AzureOpenAIProviderOptions
                {
                    Endpoint = options.AzureOpenAI.Endpoint,
                    ApiKey = options.AzureOpenAI.ApiKey,
                    DeploymentName = options.AzureOpenAI.DeploymentName,
                    Temperature = options.AzureOpenAI.Temperature,
                    MaxTokens = options.AzureOpenAI.MaxTokens,
                    EmbeddingDeploymentName = options.AzureOpenAI.EmbeddingDeploymentName
                },
                loggerFactory.CreateLogger<AzureOpenAIProvider>()),
            
            _ => new OpenAIProvider(
                new OpenAIProviderOptions
                {
                    ApiKey = options.OpenAI.ApiKey,
                    Model = options.OpenAI.Model,
                    Temperature = options.OpenAI.Temperature,
                    MaxTokens = options.OpenAI.MaxTokens,
                    EmbeddingModel = options.OpenAI.EmbeddingModel
                },
                loggerFactory.CreateLogger<OpenAIProvider>())
        };
    }
}
