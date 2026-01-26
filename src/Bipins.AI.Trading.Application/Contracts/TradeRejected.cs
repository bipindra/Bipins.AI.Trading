namespace Bipins.AI.Trading.Application.Contracts;

public record TradeRejected(
    string DecisionId,
    string Symbol,
    string Reason,
    string CorrelationId);
