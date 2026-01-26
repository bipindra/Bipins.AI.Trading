using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Web;

public class TradingDecisionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<TradingDecisionHostedService> _logger;
    
    // Keep legacy decision engine for backward compatibility
    public TradingDecisionHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<TradingDecisionHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradingDecisionHostedService started");
        
        var timeframe = new Timeframe(_tradingOptions.Timeframe);
        var interval = timeframe.ToTimeSpan();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_tradingOptions.Enabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }
                
                using var scope = _serviceScopeFactory.CreateScope();
                var decisionEngine = scope.ServiceProvider.GetRequiredService<IDecisionEngine>();
                var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
                var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                
                foreach (var symbolStr in _tradingOptions.Symbols)
                {
                    var symbol = new Symbol(symbolStr);
                    
                    // Get latest candles for decision
                    var from = DateTime.UtcNow.AddDays(-7);
                    var to = DateTime.UtcNow;
                    var candles = await candleRepository.GetCandlesAsync(symbol, timeframe, from, to, stoppingToken);
                    
                    if (candles.Count < 14)
                    {
                        _logger.LogDebug("Insufficient candles for {Symbol}, skipping decision", symbol.Value);
                        continue;
                    }
                    
                    // Get features (simplified - in production would get from FeatureSnapshot)
                    var features = new Dictionary<string, decimal>();
                    if (candles.Count >= 2)
                    {
                        var latest = candles.Last();
                        var prev = candles[candles.Count - 2];
                        features["PriceChange"] = latest.Close - prev.Close;
                        features["Volume"] = latest.Volume;
                    }
                    
                    // Get portfolio
                    var portfolio = await portfolioService.GetCurrentPortfolioAsync(stoppingToken);
                    
                    // Make decision
                    var decision = await decisionEngine.MakeDecisionAsync(
                        symbol, timeframe, candles, features, portfolio, stoppingToken);
                    
                    // Publish TradeProposed
                    var correlationId = Guid.NewGuid().ToString();
                    
                    await publishEndpoint.Publish(new TradeProposed(
                        decision.Id,
                        decision.Symbol.Value,
                        decision.Timeframe.Value,
                        decision.CandleTimestamp,
                        decision.Action.ToString(),
                        decision.QuantityPercent,
                        decision.Quantity?.Value,
                        decision.SuggestedStopLoss?.Amount,
                        decision.SuggestedTakeProfit?.Amount,
                        decision.Confidence,
                        decision.Rationale,
                        decision.Features,
                        correlationId), stoppingToken);
                    
                    _logger.LogInformation("Published TradeProposed: {Symbol} {Action} (confidence: {Confidence:F2})",
                        symbol.Value, decision.Action, decision.Confidence);
                }
                
                // Wait for next interval
                await Task.Delay(interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trading decision service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
