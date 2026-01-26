using System.Text.Json;
using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class TradeDecisionEntity
{
    public long Id { get; set; }
    public string DecisionId { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime CandleTimestamp { get; set; }
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty; // Buy, Sell, Hold
    public decimal? QuantityPercent { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? SuggestedStopLoss { get; set; }
    public decimal? SuggestedTakeProfit { get; set; }
    public decimal Confidence { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string FeaturesJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, object> GetFeatures()
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(FeaturesJson) ?? new();
    }
    
    public void SetFeatures(Dictionary<string, object> features)
    {
        FeaturesJson = JsonSerializer.Serialize(features);
    }
    
    public TradeAction GetAction() => Enum.Parse<TradeAction>(Action);
}
