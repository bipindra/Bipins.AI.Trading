// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM.Providers (will be Bipins.AI.LLM.Providers in the NuGet package)
// For now using BipinsAI namespace to avoid conflicts

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.LLM.Providers;

public class AnthropicProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-opus-20240229";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

public class AnthropicProvider : ILLMProvider, IDisposable
{
    private readonly AnthropicProviderOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public AnthropicProvider(AnthropicProviderOptions options, ILogger<AnthropicProvider> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("Anthropic API key not configured");
        }
        else
        {
            _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
    }
    
    public async Task<LLMResponse> ChatAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("Anthropic API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages
                .Where(m => m.Role != "system")
                .Select(m => new
                {
                    role = m.Role == "assistant" ? "assistant" : "user",
                    content = m.Content
                }).ToList();
            
            var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content;
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                temperature = request.Temperature ?? _options.Temperature,
                messages = messages,
                system = systemMessage
            };
            
            var response = await _httpClient.PostAsJsonAsync("messages", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(_jsonOptions, cancellationToken);
            
            return new LLMResponse
            {
                Content = result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty,
                Model = result?.Model ?? _options.Model,
                TokensUsed = (result?.Usage?.InputTokens ?? 0) + (result?.Usage?.OutputTokens ?? 0),
                FinishReason = result?.StopReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with Anthropic");
            throw;
        }
    }
    
    public async Task<LLMResponse> ChatWithFunctionsAsync(
        LLMRequest request,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("Anthropic API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages
                .Where(m => m.Role != "system")
                .Select(m => new
                {
                    role = m.Role == "assistant" ? "assistant" : "user",
                    content = m.Content
                }).ToList();
            
            var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content;
            
            var tools = functions.Select(f => new
            {
                name = f.Name,
                description = f.Description,
                input_schema = f.Parameters
            }).ToList();
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                temperature = request.Temperature ?? _options.Temperature,
                messages = messages,
                system = systemMessage,
                tools = tools
            };
            
            var response = await _httpClient.PostAsJsonAsync("messages", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(_jsonOptions, cancellationToken);
            
            var functionCalls = new List<FunctionCall>();
            if (result?.Content != null)
            {
                foreach (var content in result.Content.Where(c => c.Type == "tool_use"))
                {
                    functionCalls.Add(new FunctionCall
                    {
                        Name = content.Name ?? string.Empty,
                        Arguments = JsonSerializer.Serialize(content.Input ?? new Dictionary<string, object>(), _jsonOptions)
                    });
                }
            }
            
            return new LLMResponse
            {
                Content = result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty,
                Model = result?.Model ?? _options.Model,
                TokensUsed = (result?.Usage?.InputTokens ?? 0) + (result?.Usage?.OutputTokens ?? 0),
                FunctionCalls = functionCalls.Any() ? functionCalls : null,
                FinishReason = result?.StopReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with functions using Anthropic");
            throw;
        }
    }
    
    public async Task<StreamingLLMResponse> ChatStreamAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("Anthropic API key not configured. Please configure API key in Settings.");
        
        try
        {
            var messages = request.Messages
                .Where(m => m.Role != "system")
                .Select(m => new
                {
                    role = m.Role == "assistant" ? "assistant" : "user",
                    content = m.Content
                }).ToList();
            
            var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content;
            
            var payload = new
            {
                model = request.Model ?? _options.Model,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                temperature = request.Temperature ?? _options.Temperature,
                messages = messages,
                system = systemMessage,
                stream = true
            };
            
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("messages", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return new StreamingLLMResponse
            {
                ContentStream = StreamContentInternal(response, cancellationToken),
                Model = _options.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream chat with Anthropic");
            throw;
        }
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Anthropic doesn't have embeddings API, return empty
        _logger.LogWarning("Anthropic does not support embeddings");
        return Array.Empty<float>();
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
                AnthropicStreamChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data, _jsonOptions);
                }
                catch
                {
                    // Skip invalid JSON chunks
                }
                
                if (chunk != null && chunk.Type == "content_block_delta" && chunk.Delta?.Type == "text_delta")
                {
                    var text = chunk.Delta.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        yield return text;
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

// Anthropic API response models
internal class AnthropicMessageResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public List<AnthropicContent>? Content { get; set; }
    public string? Model { get; set; }
    public string? StopReason { get; set; }
    public AnthropicUsage? Usage { get; set; }
}

internal class AnthropicContent
{
    public string? Type { get; set; }
    public string? Text { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, object>? Input { get; set; }
}

internal class AnthropicUsage
{
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}

internal class AnthropicStreamChunk
{
    public string? Type { get; set; }
    public AnthropicStreamDelta? Delta { get; set; }
}

internal class AnthropicStreamDelta
{
    public string? Type { get; set; }
    public string? Text { get; set; }
}
