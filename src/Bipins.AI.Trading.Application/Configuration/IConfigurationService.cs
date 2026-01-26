using Bipins.AI.Trading.Application.Options;

namespace Bipins.AI.Trading.Application.Configuration;

public interface IConfigurationService
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<bool> HasValueAsync(string key, CancellationToken cancellationToken = default);
    Task<AlpacaCredentials?> GetAlpacaCredentialsAsync(CancellationToken cancellationToken = default);
    Task SetAlpacaCredentialsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    Task<LLMOptions> GetLLMOptionsAsync(CancellationToken cancellationToken = default);
    Task SetLLMOptionsAsync(LLMOptions options, CancellationToken cancellationToken = default);
    Task<VectorDbOptions> GetVectorDbOptionsAsync(CancellationToken cancellationToken = default);
    Task SetVectorDbOptionsAsync(VectorDbOptions options, CancellationToken cancellationToken = default);
}

public class AlpacaCredentials
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
