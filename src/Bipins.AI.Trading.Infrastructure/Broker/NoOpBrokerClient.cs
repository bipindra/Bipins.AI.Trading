using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Infrastructure.Broker;

internal class NoOpBrokerClient : IBrokerClient
{
    public Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AccountInfo { Cash = new Money(100000, "USD"), Equity = new Money(100000, "USD"), BuyingPower = new Money(100000, "USD") });
    
    public Task<List<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Position>());
    
    public Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Order>());
    
    public Task<Order> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Broker client not configured");
    
    public Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Broker client not configured");
}

internal class NoOpMarketDataClient : IMarketDataClient
{
    public Task<List<Candle>> GetHistoricalCandlesAsync(Symbol symbol, Timeframe timeframe, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Candle>());
    
    public Task<List<Candle>> PollLatestCandlesAsync(List<Symbol> symbols, Timeframe timeframe, DateTime? since = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Candle>());
    
    public Task<decimal> GetCurrentPriceAsync(Symbol symbol, CancellationToken cancellationToken = default)
        => Task.FromResult(100m);
}
