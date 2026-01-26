using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IBrokerClient _brokerClient;
    private readonly ILogger<PortfolioService> _logger;
    
    public PortfolioService(IBrokerClient brokerClient, ILogger<PortfolioService> logger)
    {
        _brokerClient = brokerClient;
        _logger = logger;
    }
    
    public async Task<Portfolio> GetCurrentPortfolioAsync(CancellationToken cancellationToken = default)
    {
        var account = await _brokerClient.GetAccountAsync(cancellationToken);
        var positions = await _brokerClient.GetPositionsAsync(cancellationToken);
        
        var portfolio = new Portfolio
        {
            Cash = account.Cash,
            Equity = account.Equity,
            BuyingPower = account.BuyingPower,
            Positions = positions,
            LastUpdatedAt = DateTime.UtcNow
        };
        
        // Calculate unrealized PnL
        var unrealizedPnL = Money.Zero;
        foreach (var position in positions)
        {
            if (!position.IsFlat)
            {
                var costBasis = position.CostBasis;
                var marketValue = position.MarketValue;
                unrealizedPnL = unrealizedPnL + (marketValue - costBasis);
            }
        }
        
        portfolio.UnrealizedPnL = unrealizedPnL;
        
        return portfolio;
    }
    
    public Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        portfolio.LastUpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
    
    public async Task<decimal> GetPositionPercentAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var portfolio = await GetCurrentPortfolioAsync(cancellationToken);
        var position = portfolio.GetPosition(symbol);
        
        if (position == null || position.IsFlat || portfolio.Equity.IsZero)
            return 0m;
        
        var positionValue = Math.Abs(position.MarketValue.Amount);
        var percent = (positionValue / portfolio.Equity.Amount) * 100;
        return percent;
    }
}
