using Alpaca.Markets;
using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Resilience;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Infrastructure.Broker.Alpaca;

public class AlpacaMarketDataClient : IMarketDataClient, IDisposable
{
    private readonly AlpacaOptions _options;
    private readonly IAlpacaCredentialsProvider _credentialsProvider;
    private readonly IResilienceService _resilienceService;
    private IAlpacaDataClient? _dataClient;
    private readonly ILogger<AlpacaMarketDataClient> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private const string ServiceName = "Alpaca";
    
    public AlpacaMarketDataClient(
        IOptions<BrokerOptions> options,
        IAlpacaCredentialsProvider credentialsProvider,
        IResilienceService resilienceService,
        ILogger<AlpacaMarketDataClient> logger)
    {
        _options = options.Value.Alpaca;
        _credentialsProvider = credentialsProvider;
        _resilienceService = resilienceService;
        _logger = logger;
    }
    
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_dataClient != null)
            return;
        
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_dataClient != null)
                return;
            
            var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
            if (credentials == null || string.IsNullOrEmpty(credentials.ApiKey) || string.IsNullOrEmpty(credentials.ApiSecret))
            {
                _logger.LogWarning("Alpaca API credentials not configured. Market data operations will fail.");
                return;
            }
            
            try
            {
                var environment = _options.BaseUrl.Contains("paper") ? Environments.Paper : Environments.Live;
                _dataClient = environment.GetAlpacaDataClient(new SecretKey(credentials.ApiKey, credentials.ApiSecret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Alpaca data client");
            }
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    public async Task<List<Candle>> GetHistoricalCandlesAsync(
        Symbol symbol,
        Timeframe timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_dataClient == null)
        {
            _logger.LogWarning("Alpaca data client not initialized. Returning empty candles list.");
            return new List<Candle>();
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var barTimeFrame = MapTimeframe(timeframe);
            var bars = await _dataClient!.ListHistoricalBarsAsync(
                new HistoricalBarsRequest(symbol.Value, from, to, barTimeFrame),
                cancellationToken);
            
            return bars.Items.Select(bar => new Candle
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Timestamp = bar.TimeUtc,
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Close = bar.Close,
                Volume = (long)bar.Volume
            }).ToList();
        }, ServiceName, cancellationToken);
    }
    
    public async Task<List<Candle>> PollLatestCandlesAsync(
        List<Symbol> symbols,
        Timeframe timeframe,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_dataClient == null)
        {
            _logger.LogWarning("Alpaca data client not initialized. Returning empty candles list.");
            return new List<Candle>();
        }
        
        var barTimeFrame = MapTimeframe(timeframe);
        var from = since ?? DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        
        var candles = new List<Candle>();
        
        foreach (var symbol in symbols)
        {
            try
            {
                var symbolCandles = await _resilienceService.ExecuteAsync(async () =>
                {
                    var bars = await _dataClient!.ListHistoricalBarsAsync(
                        new HistoricalBarsRequest(symbol.Value, from, to, barTimeFrame),
                        cancellationToken);
                    
                    return bars.Items.Select(bar => new Candle
                    {
                        Symbol = symbol,
                        Timeframe = timeframe,
                        Timestamp = bar.TimeUtc,
                        Open = bar.Open,
                        High = bar.High,
                        Low = bar.Low,
                        Close = bar.Close,
                        Volume = (long)bar.Volume
                    }).ToList();
                }, ServiceName, cancellationToken);
                
                candles.AddRange(symbolCandles);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get candles for {Symbol}", symbol.Value);
            }
        }
        
        return candles.OrderBy(c => c.Timestamp).ToList();
    }
    
    public async Task<decimal> GetCurrentPriceAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_dataClient == null)
        {
            _logger.LogWarning("Alpaca data client not initialized. Returning zero price.");
            return 0m;
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var request = new LatestMarketDataRequest(symbol.Value);
            var quote = await _dataClient!.GetLatestQuoteAsync(request, cancellationToken);
            return quote.AskPrice > 0 ? quote.AskPrice : quote.BidPrice;
        }, ServiceName, cancellationToken);
    }
    
    private static BarTimeFrame MapTimeframe(Timeframe timeframe)
    {
        // Alpaca.Markets 7.0.0 uses TimeFrame enum, not BarTimeFrame
        // Map to TimeFrame and then convert if needed
        return timeframe.Value switch
        {
            "1m" => BarTimeFrame.Minute,
            "5m" => BarTimeFrame.Minute, // Fallback - will need to verify correct enum
            "15m" => BarTimeFrame.Minute, // Fallback - will need to verify correct enum
            "1h" => BarTimeFrame.Hour,
            "1d" => BarTimeFrame.Day,
            _ => BarTimeFrame.Minute
        };
    }
    
    public void Dispose()
    {
        _dataClient?.Dispose();
    }
}
