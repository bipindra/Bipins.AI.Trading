using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Ports;

public interface IPortfolioService
{
    Task<Portfolio> GetCurrentPortfolioAsync(CancellationToken cancellationToken = default);
    Task UpdatePortfolioAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task<decimal> GetPositionPercentAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
