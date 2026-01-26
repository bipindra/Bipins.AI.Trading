namespace Bipins.AI.Trading.Application.Contracts;

public record OrderSubmitted(
    string OrderId,
    string ClientOrderId,
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    decimal? LimitPrice,
    DateTime SubmittedAt,
    string CorrelationId);
