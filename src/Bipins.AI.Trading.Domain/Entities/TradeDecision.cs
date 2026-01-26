using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public enum TradeAction
{
    Buy,
    Sell,
    Hold
}

public class TradeDecision
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public Symbol Symbol { get; init; } = null!;
    public Timeframe Timeframe { get; init; } = null!;
    public DateTime CandleTimestamp { get; init; }
    public DateTime DecisionTimestamp { get; init; } = DateTime.UtcNow;
    public TradeAction Action { get; init; }
    public decimal? QuantityPercent { get; init; }
    public Quantity? Quantity { get; init; }
    public Money? SuggestedStopLoss { get; init; }
    public Money? SuggestedTakeProfit { get; init; }
    public decimal Confidence { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public Dictionary<string, object> Features { get; init; } = new();
    
    public string GetIdempotencyKey() => $"{Symbol.Value}_{Timeframe.Value}_{CandleTimestamp:yyyyMMddHHmmss}";
}
