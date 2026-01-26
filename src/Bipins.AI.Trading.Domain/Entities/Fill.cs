using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Fill
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OrderId { get; init; } = null!;
    public string ClientOrderId { get; init; } = null!;
    public Symbol Symbol { get; init; } = null!;
    public OrderSide Side { get; init; }
    public Quantity Quantity { get; init; } = Quantity.Zero;
    public Money Price { get; init; } = Money.Zero;
    public Money Commission { get; init; } = Money.Zero;
    public DateTime FilledAt { get; init; } = DateTime.UtcNow;
    
    public Money TotalValue => new Money(Quantity.Value * Price.Amount, Price.Currency);
}
