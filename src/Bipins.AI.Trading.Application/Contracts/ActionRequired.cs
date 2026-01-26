namespace Bipins.AI.Trading.Application.Contracts;

public record ActionRequired(
    string DecisionId,
    string Symbol,
    string Action,
    decimal? Quantity,
    decimal Confidence,
    string Rationale,
    Dictionary<string, object> RiskChecks,
    DateTime RequestedAt,
    string CorrelationId);
