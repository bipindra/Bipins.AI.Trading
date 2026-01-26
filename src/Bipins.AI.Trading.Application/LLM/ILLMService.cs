// Application interface that matches Bipins.AI.LLM.IChatService
// This will be replaced with Bipins.AI.LLM.IChatService when NuGet package is available
// Types (LLMResponse, FunctionDefinition, FunctionCall) are defined in ILLMProvider.cs

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
