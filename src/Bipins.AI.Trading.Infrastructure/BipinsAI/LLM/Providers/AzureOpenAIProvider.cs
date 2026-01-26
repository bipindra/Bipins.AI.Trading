// This code should be moved to Bipins.AI NuGet package
// Namespace: Bipins.AI.LLM.Providers (will be Bipins.AI.LLM.Providers in the NuGet package)
// For now using BipinsAI namespace to avoid conflicts

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.LLM.Providers;

public class AzureOpenAIProviderOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
}

public class AzureOpenAIProvider : ILLMProvider, IDisposable
{
    private readonly AzureOpenAIProviderOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAIProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public AzureOpenAIProvider(AzureOpenAIProviderOptions options, ILogger<AzureOpenAIProvider> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.Endpoint))
        {
            _logger.LogWarning("Azure OpenAI API key or endpoint not configured");
        }
        else
        {
            _httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/openai/deployments/" + _options.DeploymentName + "/");
            _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
        }
    }
    
    public async Task<LLMResponse> ChatAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.Endpoint))
            throw new InvalidOperationException("Azure OpenAI API key or endpoint not configured. Please configure in Settings.");
        
        try
        {
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();
            
            var payload = new
            {
                messages = messages,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens
            };
            
            var response = await _httpClient.PostAsJsonAsync("chat/completions?api-version=2024-02-15-preview", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            // Use dynamic deserialization to avoid duplicate type definitions
            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(resultJson);
            
            var choices = result.GetProperty("choices");
            var firstChoice = choices.EnumerateArray().FirstOrDefault();
            var message = firstChoice.GetProperty("message");
            var content = message.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? string.Empty : string.Empty;
            var model = result.TryGetProperty("model", out var modelProp) ? modelProp.GetString() ?? _options.DeploymentName : _options.DeploymentName;
            var usage = result.TryGetProperty("usage", out var usageProp) ? usageProp.TryGetProperty("total_tokens", out var tokensProp) ? tokensProp.GetInt32() : (int?)null : null;
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var finishProp) ? finishProp.GetString() : null;
            
            return new LLMResponse
            {
                Content = content,
                Model = model,
                TokensUsed = usage,
                FinishReason = finishReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with Azure OpenAI");
            throw;
        }
    }
    
    public async Task<LLMResponse> ChatWithFunctionsAsync(
        LLMRequest request,
        List<FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.Endpoint))
            throw new InvalidOperationException("Azure OpenAI API key or endpoint not configured. Please configure in Settings.");
        
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
                messages = messages,
                tools = functionDefinitions,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens
            };
            
            var response = await _httpClient.PostAsJsonAsync("chat/completions?api-version=2024-02-15-preview", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(resultJson);
            
            var choices = result.GetProperty("choices");
            var firstChoice = choices.EnumerateArray().FirstOrDefault();
            var message = firstChoice.GetProperty("message");
            var content = message.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? string.Empty : string.Empty;
            var model = result.TryGetProperty("model", out var modelProp) ? modelProp.GetString() ?? _options.DeploymentName : _options.DeploymentName;
            var usage = result.TryGetProperty("usage", out var usageProp) ? usageProp.TryGetProperty("total_tokens", out var tokensProp) ? tokensProp.GetInt32() : (int?)null : null;
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var finishProp) ? finishProp.GetString() : null;
            
            var functionCalls = new List<FunctionCall>();
            if (message.TryGetProperty("tool_calls", out var toolCallsProp))
            {
                foreach (var toolCall in toolCallsProp.EnumerateArray())
                {
                    if (toolCall.TryGetProperty("function", out var funcProp))
                    {
                        var funcName = funcProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                        var funcArgs = funcProp.TryGetProperty("arguments", out var argsProp) ? argsProp.GetString() ?? string.Empty : string.Empty;
                        functionCalls.Add(new FunctionCall
                        {
                            Name = funcName,
                            Arguments = funcArgs
                        });
                    }
                }
            }
            
            return new LLMResponse
            {
                Content = content,
                Model = model,
                TokensUsed = usage,
                FunctionCalls = functionCalls.Any() ? functionCalls : null,
                FinishReason = finishReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chat with functions using Azure OpenAI");
            throw;
        }
    }
    
    public async Task<StreamingLLMResponse> ChatStreamAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.Endpoint))
            throw new InvalidOperationException("Azure OpenAI API key or endpoint not configured. Please configure in Settings.");
        
        try
        {
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();
            
            var payload = new
            {
                messages = messages,
                temperature = request.Temperature ?? _options.Temperature,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                stream = true
            };
            
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("chat/completions?api-version=2024-02-15-preview", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return new StreamingLLMResponse
            {
                ContentStream = StreamContentInternal(response, cancellationToken),
                Model = _options.DeploymentName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream chat with Azure OpenAI");
            throw;
        }
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.Endpoint))
            throw new InvalidOperationException("Azure OpenAI API key or endpoint not configured. Please configure in Settings.");
        
        try
        {
            var embeddingDeployment = _options.EmbeddingDeploymentName ?? "text-embedding-3-small";
            var baseUrl = _options.Endpoint.TrimEnd('/') + "/openai/deployments/" + embeddingDeployment + "/";
            var embeddingClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            embeddingClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
            
            var payload = new
            {
                input = text
            };
            
            var response = await embeddingClient.PostAsJsonAsync("embeddings?api-version=2024-02-15-preview", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(resultJson);
            
            embeddingClient.Dispose();
            
            if (result.TryGetProperty("data", out var dataProp))
            {
                var firstItem = dataProp.EnumerateArray().FirstOrDefault();
                if (firstItem.TryGetProperty("embedding", out var embeddingProp))
                {
                    var embedding = embeddingProp.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                    return embedding;
                }
            }
            
            return Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding with Azure OpenAI");
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
                System.Text.Json.JsonElement? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(data);
                }
                catch
                {
                    // Skip invalid JSON chunks
                }
                
                if (chunk.HasValue)
                {
                    var chunkValue = chunk.Value;
                    if (chunkValue.TryGetProperty("choices", out var choicesProp))
                    {
                        var firstChoice = choicesProp.EnumerateArray().FirstOrDefault();
                        if (firstChoice.TryGetProperty("delta", out var deltaProp))
                        {
                            if (deltaProp.TryGetProperty("content", out var contentProp))
                            {
                                var delta = contentProp.GetString();
                                if (!string.IsNullOrEmpty(delta))
                                {
                                    yield return delta;
                                }
                            }
                        }
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

// Note: Azure OpenAI uses the same response format as OpenAI
// Response model classes are defined in OpenAIProvider.cs to avoid duplication
