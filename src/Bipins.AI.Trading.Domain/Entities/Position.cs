using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Position
{
    public Symbol Symbol { get; init; } = null!;
    public Quantity Quantity { get; init; } = Quantity.Zero;
    public Money AveragePrice { get; init; } = Money.Zero;
    public Money CurrentPrice { get; set; } = Money.Zero;
    public Money UnrealizedPnL { get; set; } = Money.Zero;
    public DateTime OpenedAt { get; init; }
    public DateTime? LastUpdatedAt { get; set; }
    
    public bool IsLong => Quantity.Value > 0;
    public bool IsShort => Quantity.Value < 0;
    public bool IsFlat => Quantity.IsZero;
    
    public Money MarketValue => new Money(Quantity.Value * CurrentPrice.Amount, CurrentPrice.Currency);
    public Money CostBasis => new Money(Quantity.Value * AveragePrice.Amount, AveragePrice.Currency);
}
