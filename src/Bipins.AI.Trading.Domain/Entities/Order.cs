using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit,
    Stop,
    StopLimit
}

public enum OrderStatus
{
    Pending,
    Submitted,
    PartiallyFilled,
    Filled,
    Canceled,
    Rejected
}

public class Order
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string ClientOrderId { get; init; } = Guid.NewGuid().ToString();
    public Symbol Symbol { get; init; } = null!;
    public OrderSide Side { get; init; }
    public OrderType Type { get; init; }
    public Quantity Quantity { get; init; } = Quantity.Zero;
    public Money? LimitPrice { get; init; }
    public Money? StopPrice { get; init; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public string? RejectionReason { get; set; }
}
