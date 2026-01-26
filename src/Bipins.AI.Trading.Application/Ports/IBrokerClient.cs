using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Ports;

public class AccountInfo
{
    public Money Cash { get; init; } = Money.Zero;
    public Money Equity { get; init; } = Money.Zero;
    public Money BuyingPower { get; init; } = Money.Zero;
    public string AccountNumber { get; init; } = string.Empty;
}

public class OrderRequest
{
    public string ClientOrderId { get; init; } = Guid.NewGuid().ToString();
    public Symbol Symbol { get; init; } = null!;
    public OrderSide Side { get; init; }
    public OrderType Type { get; init; }
    public Quantity Quantity { get; init; } = Quantity.Zero;
    public Money? LimitPrice { get; init; }
    public Money? StopPrice { get; init; }
    public TimeSpan? TimeInForce { get; init; }
}

public interface IBrokerClient
{
    Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default);
    Task<List<Position>> GetPositionsAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<Order> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
