namespace Bipins.AI.Trading.Application.Indicators;

public abstract class IndicatorResult
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
