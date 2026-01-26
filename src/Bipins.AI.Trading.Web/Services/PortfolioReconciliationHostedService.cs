using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Ports;
using MassTransit;

namespace Bipins.AI.Trading.Web;

public class PortfolioReconciliationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PortfolioReconciliationHostedService> _logger;
    
    public PortfolioReconciliationHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PortfolioReconciliationHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PortfolioReconciliationHostedService started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                
                // Reconcile portfolio every minute
                var portfolio = await portfolioService.GetCurrentPortfolioAsync(stoppingToken);
                await portfolioService.UpdatePortfolioAsync(portfolio, stoppingToken);
                
                var correlationId = Guid.NewGuid().ToString();
                
                await publishEndpoint.Publish(new PortfolioUpdated(
                    portfolio.Cash.Amount,
                    portfolio.Equity.Amount,
                    portfolio.BuyingPower.Amount,
                    portfolio.UnrealizedPnL.Amount,
                    portfolio.RealizedPnL.Amount,
                    portfolio.Positions.Count,
                    portfolio.LastUpdatedAt,
                    correlationId), stoppingToken);
                
                _logger.LogDebug("Published PortfolioUpdated: Equity={Equity:C}, Cash={Cash:C}",
                    portfolio.Equity.Amount, portfolio.Cash.Amount);
                
                // Wait 1 minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in portfolio reconciliation");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
