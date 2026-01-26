namespace Bipins.AI.Trading.Application.Contracts;

public record FeedDisconnected(
    string Source,
    string Reason,
    DateTime DisconnectedAt,
    string CorrelationId);
