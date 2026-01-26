using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Domain.Entities;

public class Portfolio
{
    public Money Cash { get; set; } = Money.Zero;
    public Money Equity { get; set; } = Money.Zero;
    public Money BuyingPower { get; set; } = Money.Zero;
    public Money UnrealizedPnL { get; set; } = Money.Zero;
    public Money RealizedPnL { get; set; } = Money.Zero;
    public Money TotalPnL => UnrealizedPnL + RealizedPnL;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<Position> Positions { get; init; } = new();
    
    public Position? GetPosition(Symbol symbol) =>
        Positions.FirstOrDefault(p => p.Symbol.Value == symbol.Value);
}
