using System.Text.Json;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class FeatureSnapshotEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime CandleTimestamp { get; set; }
    public string FeaturesJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, decimal> GetFeatures()
    {
        return JsonSerializer.Deserialize<Dictionary<string, decimal>>(FeaturesJson) ?? new();
    }
    
    public void SetFeatures(Dictionary<string, decimal> features)
    {
        FeaturesJson = JsonSerializer.Serialize(features);
    }
}
