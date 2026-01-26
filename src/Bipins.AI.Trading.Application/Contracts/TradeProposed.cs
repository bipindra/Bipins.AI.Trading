namespace Bipins.AI.Trading.Application.Contracts;

public record TradeProposed(
    string DecisionId,
    string Symbol,
    string Timeframe,
    DateTime CandleTimestamp,
    string Action,
    decimal? QuantityPercent,
    decimal? Quantity,
    decimal? SuggestedStopLoss,
    decimal? SuggestedTakeProfit,
    decimal Confidence,
    string Rationale,
    Dictionary<string, object> Features,
    string CorrelationId);
