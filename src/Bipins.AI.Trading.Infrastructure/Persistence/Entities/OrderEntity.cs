using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class OrderEntity
{
    public long Id { get; set; }
    public string OrderId { get; set; } = Guid.NewGuid().ToString();
    public string ClientOrderId { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty; // Buy, Sell
    public string Type { get; set; } = string.Empty; // Market, Limit, etc.
    public decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Submitted, Filled, etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public string? RejectionReason { get; set; }
    
    public OrderSide GetSide() => Enum.Parse<OrderSide>(Side);
    public OrderType GetType() => Enum.Parse<OrderType>(Type);
    public OrderStatus GetStatus() => Enum.Parse<OrderStatus>(Status);
}
