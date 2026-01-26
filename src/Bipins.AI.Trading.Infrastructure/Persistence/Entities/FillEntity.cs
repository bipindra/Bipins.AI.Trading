using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Infrastructure.Persistence.Entities;

public class FillEntity
{
    public long Id { get; set; }
    public string FillId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    public string ClientOrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty; // Buy, Sell
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public DateTime FilledAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public OrderSide GetSide() => Enum.Parse<OrderSide>(Side);
}
