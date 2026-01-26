namespace Bipins.AI.Trading.Application.Contracts;

public record CandleClosed(
    string Symbol,
    string Timeframe,
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    string CorrelationId);
