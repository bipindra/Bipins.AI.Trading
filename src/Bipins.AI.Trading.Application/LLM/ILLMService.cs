// Application interface that matches Bipins.AI.LLM.IChatService, but keeps
// our domain-specific response types for now.

namespace Bipins.AI.Trading.Application.LLM;

public interface ILLMService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<LLMResponse> ChatWithFunctionsAsync(
        string systemPrompt,
        string userMessage,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default);
    Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingVectorAsync(string text, CancellationToken cancellationToken = default);
}

// Local LLM DTOs, previously defined in ILLMProvider.cs
public class LLMRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? Model { get; set; }
}

public class LLMResponse
{
    public string Content { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int? TokensUsed { get; set; }
    public List<FunctionCall>? FunctionCalls { get; set; }
    public string? FinishReason { get; set; }
}

public class StreamingLLMResponse
{
    public IAsyncEnumerable<string> ContentStream { get; set; } = null!;
    public string? Model { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "system", "user", "assistant", "function"
    public string Content { get; set; } = string.Empty;
    public string? Name { get; set; } // For function calls
    public FunctionCall? FunctionCall { get; set; }
}

public class FunctionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new(); // JSON schema
}

public class FunctionCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty; // JSON string
}
