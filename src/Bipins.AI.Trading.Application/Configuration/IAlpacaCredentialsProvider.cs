namespace Bipins.AI.Trading.Application.Configuration;

public interface IAlpacaCredentialsProvider
{
    Task<AlpacaCredentials?> GetCredentialsAsync(CancellationToken cancellationToken = default);
    bool HasCredentials();
    void InvalidateCache();
}
