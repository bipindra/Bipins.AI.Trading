namespace Bipins.AI.Trading.Application.Contracts;

public record IndicatorsCalculated(
    string Symbol,
    string Timeframe,
    DateTime CandleTimestamp,
    Dictionary<string, Dictionary<string, object>> Indicators,
    string CorrelationId);
