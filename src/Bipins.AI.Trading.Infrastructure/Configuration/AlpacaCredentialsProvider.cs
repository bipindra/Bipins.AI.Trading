using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Infrastructure.Configuration;

public class AlpacaCredentialsProvider : IAlpacaCredentialsProvider
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly BrokerOptions _brokerOptions;
    private readonly ILogger<AlpacaCredentialsProvider> _logger;
    private AlpacaCredentials? _cachedCredentials;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    
    public AlpacaCredentialsProvider(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<BrokerOptions> brokerOptions,
        ILogger<AlpacaCredentialsProvider> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _brokerOptions = brokerOptions.Value;
        _logger = logger;
    }
    
    public async Task<AlpacaCredentials?> GetCredentialsAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cachedCredentials != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedCredentials;
        }
        
        // Try database first - create a scope to access scoped service
        using var scope = _serviceScopeFactory.CreateScope();
        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        var dbCredentials = await configurationService.GetAlpacaCredentialsAsync(cancellationToken);
        if (dbCredentials != null && !string.IsNullOrEmpty(dbCredentials.ApiKey) && !string.IsNullOrEmpty(dbCredentials.ApiSecret))
        {
            _cachedCredentials = dbCredentials;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
            return dbCredentials;
        }
        
        // Fall back to appsettings
        if (!string.IsNullOrEmpty(_brokerOptions.Alpaca.ApiKey) && !string.IsNullOrEmpty(_brokerOptions.Alpaca.ApiSecret))
        {
            _cachedCredentials = new AlpacaCredentials
            {
                ApiKey = _brokerOptions.Alpaca.ApiKey,
                ApiSecret = _brokerOptions.Alpaca.ApiSecret
            };
            _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
            return _cachedCredentials;
        }
        
        return null;
    }
    
    public bool HasCredentials()
    {
        // Quick check without async - check appsettings only
        return !string.IsNullOrEmpty(_brokerOptions.Alpaca.ApiKey) && 
               !string.IsNullOrEmpty(_brokerOptions.Alpaca.ApiSecret);
    }
    
    public void InvalidateCache()
    {
        _cachedCredentials = null;
        _cacheExpiry = DateTime.MinValue;
    }
}
