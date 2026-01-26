namespace Bipins.AI.Trading.Application.Contracts;

public record PortfolioUpdated(
    decimal Cash,
    decimal Equity,
    decimal BuyingPower,
    decimal UnrealizedPnL,
    decimal RealizedPnL,
    int PositionCount,
    DateTime UpdatedAt,
    string CorrelationId);
