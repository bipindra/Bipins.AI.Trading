namespace Bipins.AI.Trading.Application.Contracts;

public record OrderCanceled(
    string OrderId,
    string ClientOrderId,
    string Symbol,
    DateTime CanceledAt,
    string CorrelationId);
