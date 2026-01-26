// Adapter to bridge Bipins.AI.LLM (from BipinsAI folder) to Application.LLM interfaces
// This adapter will be removed once Bipins.AI NuGet package is available

using AppLLM = Bipins.AI.Trading.Application.LLM;

namespace Bipins.AI.Trading.Infrastructure.LLM;

public class BipinsAIAdapter : AppLLM.ILLMProvider
{
    private readonly Bipins.AI.LLM.ILLMProvider _bipinsAIProvider;
    
    public BipinsAIAdapter(Bipins.AI.LLM.ILLMProvider bipinsAIProvider)
    {
        _bipinsAIProvider = bipinsAIProvider;
    }
    
    public async Task<AppLLM.LLMResponse> ChatAsync(AppLLM.LLMRequest request, CancellationToken cancellationToken = default)
    {
        var bipinsRequest = new Bipins.AI.LLM.LLMRequest
        {
            Messages = request.Messages.Select(m => new Bipins.AI.LLM.ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name,
                FunctionCall = m.FunctionCall != null ? new Bipins.AI.LLM.FunctionCall
                {
                    Name = m.FunctionCall.Name,
                    Arguments = m.FunctionCall.Arguments
                } : null
            }).ToList(),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Model = request.Model
        };
        
        var response = await _bipinsAIProvider.ChatAsync(bipinsRequest, cancellationToken);
        
        return new AppLLM.LLMResponse
        {
            Content = response.Content,
            Model = response.Model,
            TokensUsed = response.TokensUsed,
            FunctionCalls = response.FunctionCalls?.Select(fc => new AppLLM.FunctionCall
            {
                Name = fc.Name,
                Arguments = fc.Arguments
            }).ToList(),
            FinishReason = response.FinishReason
        };
    }
    
    public async Task<AppLLM.LLMResponse> ChatWithFunctionsAsync(
        AppLLM.LLMRequest request,
        List<AppLLM.FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        var bipinsRequest = new Bipins.AI.LLM.LLMRequest
        {
            Messages = request.Messages.Select(m => new Bipins.AI.LLM.ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name,
                FunctionCall = m.FunctionCall != null ? new Bipins.AI.LLM.FunctionCall
                {
                    Name = m.FunctionCall.Name,
                    Arguments = m.FunctionCall.Arguments
                } : null
            }).ToList(),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Model = request.Model
        };
        
        var bipinsFunctions = functions.Select(f => new Bipins.AI.LLM.FunctionDefinition
        {
            Name = f.Name,
            Description = f.Description,
            Parameters = f.Parameters
        }).ToList();
        
        var response = await _bipinsAIProvider.ChatWithFunctionsAsync(bipinsRequest, bipinsFunctions, cancellationToken);
        
        return new AppLLM.LLMResponse
        {
            Content = response.Content,
            Model = response.Model,
            TokensUsed = response.TokensUsed,
            FunctionCalls = response.FunctionCalls?.Select(fc => new AppLLM.FunctionCall
            {
                Name = fc.Name,
                Arguments = fc.Arguments
            }).ToList(),
            FinishReason = response.FinishReason
        };
    }
    
    public async Task<AppLLM.StreamingLLMResponse> ChatStreamAsync(AppLLM.LLMRequest request, CancellationToken cancellationToken = default)
    {
        var bipinsRequest = new Bipins.AI.LLM.LLMRequest
        {
            Messages = request.Messages.Select(m => new Bipins.AI.LLM.ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name
            }).ToList(),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Model = request.Model
        };
        
        var response = await _bipinsAIProvider.ChatStreamAsync(bipinsRequest, cancellationToken);
        
        return new AppLLM.StreamingLLMResponse
        {
            ContentStream = response.ContentStream,
            Model = response.Model
        };
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _bipinsAIProvider.GenerateEmbeddingAsync(text, cancellationToken);
    }
}
