namespace Bipins.AI.Trading.Application.Options;

public class LLMOptions
{
    public const string SectionName = "LLM";
    
    public string Provider { get; set; } = "OpenAI"; // OpenAI, Anthropic, AzureOpenAI
    
    public OpenAIOptions OpenAI { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
    public AzureOpenAIOptions AzureOpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public class AnthropicOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-opus-20240229";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
}
