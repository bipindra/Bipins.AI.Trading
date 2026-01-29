// Adapter to bridge Bipins.AI.LLM.IChatService to Application.LLM.ILLMService

using System.Text.Json;
using AppLLM = Bipins.AI.Trading.Application.LLM;

namespace Bipins.AI.Trading.Infrastructure.LLM;

public class BipinsAIChatServiceAdapter : AppLLM.ILLMService
{
    private readonly Bipins.AI.LLM.IChatService _bipinsChatService;
    
    public BipinsAIChatServiceAdapter(Bipins.AI.LLM.IChatService bipinsChatService)
    {
        _bipinsChatService = bipinsChatService;
    }
    
    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        return await _bipinsChatService.ChatAsync(systemPrompt, userMessage, cancellationToken);
    }
    
    public async Task<AppLLM.LLMResponse> ChatWithFunctionsAsync(
        string systemPrompt,
        string userMessage,
        List<AppLLM.FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        var tools = functions.Select(f =>
            new Bipins.AI.Core.Models.ToolDefinition(
                f.Name,
                f.Description,
                JsonSerializer.SerializeToElement(f.Parameters)))
            .ToList();

        var response = await _bipinsChatService.ChatWithToolsAsync(systemPrompt, userMessage, tools, cancellationToken);

        return new AppLLM.LLMResponse
        {
            Content = response.Content,
            Model = response.ModelId,
            TokensUsed = response.Usage.TotalTokens,
            FunctionCalls = response.ToolCalls?.Select(tc => new AppLLM.FunctionCall
            {
                Name = tc.Name,
                Arguments = tc.Arguments.GetRawText()
            }).ToList(),
            FinishReason = response.FinishReason
        };
    }
    
    public async Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // For now, return a placeholder - embeddings will be handled by provider directly
        return string.Empty;
    }
    
    public async Task<float[]> GenerateEmbeddingVectorAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _bipinsChatService.GenerateEmbeddingAsync(text, cancellationToken);
    }
}
