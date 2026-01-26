namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class AgentEventEntity
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty; // RiskBreach, FeedDisconnected, Error, etc.
    public string Message { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? CorrelationId { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
