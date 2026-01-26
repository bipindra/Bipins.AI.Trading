using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Ports;

public class RiskCheckResult
{
    public bool IsAllowed { get; init; }
    public string? Reason { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public interface IRiskManager
{
    Task<RiskCheckResult> CheckTradeAsync(TradeDecision decision, Portfolio portfolio, CancellationToken cancellationToken = default);
    Task<RiskCheckResult> CheckOrderAsync(Order order, Portfolio portfolio, CancellationToken cancellationToken = default);
    Task<bool> CheckDailyLossLimitAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    Task<bool> CheckMaxPositionsAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
}
