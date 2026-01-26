// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM.Providers (will be Bipins.AI.LLM.Providers in the NuGet package)
// For now using BipinsAI namespace to avoid conflicts

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.LLM.Providers;

public class OpenAIProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public class OpenAIProvider : ILLMProvider, IDisposable
{
    private readonly OpenAIProviderOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public OpenAIProvider(OpenAIProviderOptions options, ILogger<OpenAIProvider> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured");
        }
        else
        {
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }
    }
    
    public async Task<LLMResponse> ChatAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                messages = messages,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens
            };
            
            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(_jsonOptions, cancellationToken);
            
            return new LLMResponse
            {
                Content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                Model = result?.Model ?? _options.Model,
                TokensUsed = result?.Usage?.TotalTokens,
                FinishReason = result?.Choices?.FirstOrDefault()?.FinishReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with OpenAI");
            throw;
        }
    }
    
    public async Task<LLMResponse> ChatWithFunctionsAsync(
        LLMRequest request,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();
            
            var functionDefinitions = functions.Select(f => new
            {
                type = "function",
                function = new
                {
                    name = f.Name,
                    description = f.Description,
                    parameters = f.Parameters
                }
            }).ToList();
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                messages = messages,
                tools = functionDefinitions,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens
            };
            
            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(_jsonOptions, cancellationToken);
            var choice = result?.Choices?.FirstOrDefault();
            
            var functionCalls = new List<FunctionCall>();
            if (choice?.Message?.ToolCalls != null)
            {
                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    functionCalls.Add(new FunctionCall
                    {
                        Name = toolCall.Function?.Name ?? string.Empty,
                        Arguments = toolCall.Function?.Arguments ?? string.Empty
                    });
                }
            }
            
            return new LLMResponse
            {
                Content = choice?.Message?.Content ?? string.Empty,
                Model = result?.Model ?? _options.Model,
                TokensUsed = result?.Usage?.TotalTokens,
                FunctionCalls = functionCalls.Any() ? functionCalls : null,
                FinishReason = choice?.FinishReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with functions using OpenAI");
            throw;
        }
    }
    
    public async Task<StreamingLLMResponse> ChatStreamAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                messages = messages,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                stream = true
            };
            
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return new StreamingLLMResponse
            {
                ContentStream = StreamContentInternal(response, cancellationToken),
                Model = _options.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream chat with OpenAI");
            throw;
        }
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Please configure API key in Settings.");
        
        try
        {
            var payload = new
            {
                model = _options.EmbeddingModel ?? "text-embedding-3-small",
                input = text
            };
            
            var response = await _httpClient.PostAsJsonAsync("embeddings", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(_jsonOptions, cancellationToken);
            
            return result?.Data?.FirstOrDefault()?.Embedding?.ToArray() ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding with OpenAI");
            throw;
        }
    }
    
    private async IAsyncEnumerable<string> StreamContentInternal(HttpResponseMessage response, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
            {
                var data = line.Substring(6);
                OpenAIChatChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAIChatChunk>(data, _jsonOptions);
                }
                catch
                {
                    // Skip invalid JSON chunks
                }
                
                if (chunk != null)
                {
                    var delta = chunk.Choices?.FirstOrDefault()?.Delta?.Content;
                    if (!string.IsNullOrEmpty(delta))
                    {
                        yield return delta;
                    }
                }
            }
        }
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// OpenAI API response models
internal class OpenAIChatResponse
{
    public string? Id { get; set; }
    public string? Model { get; set; }
    public List<OpenAIChatChoice>? Choices { get; set; }
    public OpenAIUsage? Usage { get; set; }
}

internal class OpenAIChatChoice
{
    public int? Index { get; set; }
    public OpenAIChatMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

internal class OpenAIChatMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
    public List<OpenAIToolCall>? ToolCalls { get; set; }
}

internal class OpenAIToolCall
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public OpenAIFunctionCall? Function { get; set; }
}

internal class OpenAIFunctionCall
{
    public string? Name { get; set; }
    public string? Arguments { get; set; }
}

internal class OpenAIUsage
{
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}

internal class OpenAIChatChunk
{
    public List<OpenAIChatChunkChoice>? Choices { get; set; }
}

internal class OpenAIChatChunkChoice
{
    public OpenAIChatDelta? Delta { get; set; }
}

internal class OpenAIChatDelta
{
    public string? Content { get; set; }
}

internal class OpenAIEmbeddingResponse
{
    public List<OpenAIEmbeddingData>? Data { get; set; }
}

internal class OpenAIEmbeddingData
{
    public float[]? Embedding { get; set; }
}
