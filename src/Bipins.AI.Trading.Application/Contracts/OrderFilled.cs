namespace Bipins.AI.Trading.Application.Contracts;

public record OrderFilled(
    string FillId,
    string OrderId,
    string ClientOrderId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Commission,
    DateTime FilledAt,
    string CorrelationId);
