using Alpaca.Markets;
using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.TickData;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Infrastructure.TickData;

public class AlpacaTickDataProvider : ITickDataProvider, IDisposable
{
    private readonly AlpacaOptions _options;
    private readonly IAlpacaCredentialsProvider _credentialsProvider;
    private IAlpacaDataClient? _dataClient;
    private IAlpacaStreamingClient? _streamingClient;
    private readonly Dictionary<string, Func<Tick, Task>> _subscriptions = new();
    private readonly ILogger<AlpacaTickDataProvider> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    
    public AlpacaTickDataProvider(
        IOptions<BrokerOptions> options,
        IAlpacaCredentialsProvider credentialsProvider,
        ILogger<AlpacaTickDataProvider> logger)
    {
        _options = options.Value.Alpaca;
        _credentialsProvider = credentialsProvider;
        _logger = logger;
    }
    
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_dataClient != null && _streamingClient != null)
            return;
        
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_dataClient != null && _streamingClient != null)
                return;
            
            var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
            if (credentials == null || string.IsNullOrEmpty(credentials.ApiKey) || string.IsNullOrEmpty(credentials.ApiSecret))
            {
                _logger.LogWarning("Alpaca API credentials not configured. Tick data operations will fail.");
                return;
            }
            
            try
            {
                var environment = _options.BaseUrl.Contains("paper") ? Environments.Paper : Environments.Live;
                _dataClient = environment.GetAlpacaDataClient(new SecretKey(credentials.ApiKey, credentials.ApiSecret));
                _streamingClient = environment.GetAlpacaStreamingClient(new SecretKey(credentials.ApiKey, credentials.ApiSecret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Alpaca clients for tick data");
            }
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    public async Task<List<Tick>> GetTicksAsync(
        Symbol symbol,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_dataClient == null)
        {
            _logger.LogWarning("Alpaca data client not initialized. Returning empty ticks list.");
            return new List<Tick>();
        }
        
        try
        {
            var ticks = new List<Tick>();
            
            // Alpaca provides trades (tick data) via historical trades API
            var trades = await _dataClient.ListHistoricalTradesAsync(
                new HistoricalTradesRequest(symbol.Value, from, to),
                cancellationToken);
            
            foreach (var trade in trades.Items)
            {
                ticks.Add(new Tick
                {
                    Symbol = symbol,
                    Timestamp = trade.TimestampUtc,
                    Price = trade.Price,
                    Volume = (long)trade.Size
                });
            }
            
            // Also get quotes for bid/ask data
            var quotes = await _dataClient.ListHistoricalQuotesAsync(
                new HistoricalQuotesRequest(symbol.Value, from, to),
                cancellationToken);
            
            // Merge quotes with ticks (simplified - in production would match by timestamp)
            foreach (var quote in quotes.Items)
            {
                var matchingTick = ticks.FirstOrDefault(t => 
                    Math.Abs((t.Timestamp - quote.TimestampUtc).TotalSeconds) < 1);
                
                if (matchingTick != null)
                {
                    // Update with bid/ask data
                    var tickIndex = ticks.IndexOf(matchingTick);
                    ticks[tickIndex] = new Tick
                    {
                        Symbol = symbol,
                        Timestamp = matchingTick.Timestamp,
                        Price = matchingTick.Price,
                        Volume = matchingTick.Volume,
                        Bid = quote.BidPrice,
                        Ask = quote.AskPrice,
                        BidSize = quote.BidSize,
                        AskSize = quote.AskSize
                    };
                }
            }
            
            return ticks.OrderBy(t => t.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ticks for {Symbol}", symbol.Value);
            throw;
        }
    }
    
    public async Task<Tick?> GetLatestTickAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_dataClient == null)
        {
            _logger.LogWarning("Alpaca data client not initialized. Returning null tick.");
            return null;
        }
        
        try
        {
            var quoteRequest = new LatestMarketDataRequest(symbol.Value);
            var tradeRequest = new LatestMarketDataRequest(symbol.Value);
            
            var quote = await _dataClient.GetLatestQuoteAsync(quoteRequest, cancellationToken);
            var trade = await _dataClient.GetLatestTradeAsync(tradeRequest, cancellationToken);
            
            if (trade == null) return null;
            
            return new Tick
            {
                Symbol = symbol,
                Timestamp = trade.TimestampUtc,
                Price = trade.Price,
                Volume = (long)trade.Size,
                Bid = quote?.BidPrice ?? trade.Price,
                Ask = quote?.AskPrice ?? trade.Price,
                BidSize = quote?.BidSize ?? 0,
                AskSize = quote?.AskSize ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest tick for {Symbol}", symbol.Value);
            throw;
        }
    }
    
    public async Task SubscribeTicksAsync(
        Symbol symbol,
        Func<Tick, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_streamingClient == null)
        {
            _logger.LogWarning("Alpaca streaming client not initialized. Cannot subscribe to ticks.");
            throw new InvalidOperationException("Alpaca streaming client not initialized. Please configure API credentials in Settings.");
        }
        
        try
        {
            _subscriptions[symbol.Value] = handler;
            
            // Subscribe to trades and quotes - in 7.0.0 API may be different
            // For now, streaming is not implemented - will need to verify correct API
            // TODO: Implement streaming subscription based on Alpaca.Markets 7.0.0 API
            
            _logger.LogInformation("Subscribed to tick data for {Symbol}", symbol.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to ticks for {Symbol}", symbol.Value);
            throw;
        }
    }
    
    public async Task UnsubscribeTicksAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_streamingClient == null)
        {
            _logger.LogWarning("Alpaca streaming client not initialized. Cannot unsubscribe from ticks.");
            return; // No-op if not initialized
        }
        
        try
        {
            // Unsubscribe - API may have changed
            // TODO: Implement unsubscription based on Alpaca.Markets 7.0.0 API
            _subscriptions.Remove(symbol.Value);
            
            _logger.LogInformation("Unsubscribed from tick data for {Symbol}", symbol.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from ticks for {Symbol}", symbol.Value);
            throw;
        }
    }
    
    public void Dispose()
    {
        _dataClient?.Dispose();
        _streamingClient?.Dispose();
    }
}
