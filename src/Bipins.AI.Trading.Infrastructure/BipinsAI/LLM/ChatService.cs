// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM

namespace Bipins.AI.LLM;

public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly ChatServiceOptions _options;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(ILLMProvider llmProvider, ChatServiceOptions options, ILogger<ChatService> logger)
    {
        _llmProvider = llmProvider;
        _options = options;
        _logger = logger;
    }
    
    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = new LLMRequest
        {
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            },
            Temperature = _options.Temperature,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens
        };
        
        var response = await _llmProvider.ChatAsync(request, cancellationToken);
        return response.Content;
    }
    
    public async Task<LLMResponse> ChatWithFunctionsAsync(
        string systemPrompt,
        string userMessage,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        var request = new LLMRequest
        {
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            },
            Temperature = _options.Temperature,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens
        };
        
        return await _llmProvider.ChatWithFunctionsAsync(request, functions, cancellationToken);
    }
    
    public async Task<IAsyncEnumerable<string>> ChatStreamAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var request = new LLMRequest
        {
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            },
            Temperature = _options.Temperature,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens
        };
        
        var response = await _llmProvider.ChatStreamAsync(request, cancellationToken);
        return response.ContentStream;
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _llmProvider.GenerateEmbeddingAsync(text, cancellationToken);
    }
}

public class ChatServiceOptions
{
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
