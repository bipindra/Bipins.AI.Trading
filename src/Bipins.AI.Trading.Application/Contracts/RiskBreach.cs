namespace Bipins.AI.Trading.Application.Contracts;

public record RiskBreach(
    string Rule,
    string Message,
    string? Symbol,
    DateTime DetectedAt,
    string CorrelationId);
