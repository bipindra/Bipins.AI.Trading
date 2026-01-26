namespace Bipins.AI.Trading.Application.Contracts;

public record TradeApproved(
    string DecisionId,
    string Symbol,
    string Action,
    decimal? Quantity,
    decimal? SuggestedStopLoss,
    string? ApprovedBy,
    DateTime ApprovedAt,
    string CorrelationId);
