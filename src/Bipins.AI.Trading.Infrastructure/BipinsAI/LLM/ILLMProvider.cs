// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM

namespace Bipins.AI.LLM;

public interface ILLMProvider
{
    Task<LLMResponse> ChatAsync(LLMRequest request, CancellationToken cancellationToken = default);
    Task<LLMResponse> ChatWithFunctionsAsync(
        LLMRequest request, 
        List<FunctionDefinition> functions, 
        CancellationToken cancellationToken = default);
    Task<StreamingLLMResponse> ChatStreamAsync(LLMRequest request, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

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
