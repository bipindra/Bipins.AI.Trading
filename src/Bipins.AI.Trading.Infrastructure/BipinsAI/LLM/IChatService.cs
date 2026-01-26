// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM

namespace Bipins.AI.LLM;

public interface IChatService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<LLMResponse> ChatWithFunctionsAsync(
        string systemPrompt,
        string userMessage,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default);
    Task<IAsyncEnumerable<string>> ChatStreamAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
