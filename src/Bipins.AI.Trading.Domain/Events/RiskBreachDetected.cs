namespace Bipins.AI.Trading.Domain.Events;

public record RiskBreachDetected(string Rule, string Message, DateTime DetectedAt) : IDomainEvent;
