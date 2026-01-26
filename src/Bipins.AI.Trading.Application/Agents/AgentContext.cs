using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Agents;

public class AgentContext
{
    public Symbol Symbol { get; set; } = null!;
    public Timeframe Timeframe { get; set; } = null!;
    public List<Candle> Candles { get; set; } = new();
    public Dictionary<string, decimal> Features { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>> Indicators { get; set; } = new();
    public Portfolio Portfolio { get; set; } = null!;
    public List<TradingScenario> SimilarScenarios { get; set; } = new();
    public List<TradeDecision> TradingHistory { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TradingScenario
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> MarketConditions { get; set; } = new();
    public string Decision { get; set; } = string.Empty; // "Buy", "Sell", "Hold"
    public decimal Confidence { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public decimal? OutcomePnL { get; set; }
    public bool? WasSuccessful { get; set; }
    public float SimilarityScore { get; set; }
}
