namespace Bipins.AI.Trading.Application.Contracts;

public record FeaturesComputed(
    string Symbol,
    string Timeframe,
    DateTime CandleTimestamp,
    Dictionary<string, decimal> Features,
    string CorrelationId);
