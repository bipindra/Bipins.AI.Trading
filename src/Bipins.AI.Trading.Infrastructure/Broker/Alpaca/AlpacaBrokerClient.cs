using Alpaca.Markets;
using AlpacaOrderStatus = Alpaca.Markets.OrderStatus;
using AlpacaOrderType = Alpaca.Markets.OrderType;
using AlpacaOrderSide = Alpaca.Markets.OrderSide;
using Bipins.AI.Trading.Application.Configuration;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Infrastructure.Resilience;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Infrastructure.Broker.Alpaca;

public class AlpacaBrokerClient : IBrokerClient, IDisposable
{
    private readonly AlpacaOptions _options;
    private readonly IAlpacaCredentialsProvider _credentialsProvider;
    private readonly IResilienceService _resilienceService;
    private IAlpacaTradingClient? _tradingClient;
    private IAlpacaDataClient? _dataClient;
    private readonly ILogger<AlpacaBrokerClient> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private const string ServiceName = "Alpaca";
    
    public AlpacaBrokerClient(
        IOptions<BrokerOptions> options,
        IAlpacaCredentialsProvider credentialsProvider,
        IResilienceService resilienceService,
        ILogger<AlpacaBrokerClient> logger)
    {
        _options = options.Value.Alpaca;
        _credentialsProvider = credentialsProvider;
        _resilienceService = resilienceService;
        _logger = logger;
    }
    
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_tradingClient != null && _dataClient != null)
            return;
        
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_tradingClient != null && _dataClient != null)
                return;
            
            var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
            if (credentials == null || string.IsNullOrEmpty(credentials.ApiKey) || string.IsNullOrEmpty(credentials.ApiSecret))
            {
                _logger.LogWarning("Alpaca API credentials not configured. Broker operations will fail.");
                return;
            }
            
            try
            {
                var environment = _options.BaseUrl.Contains("paper") ? Environments.Paper : Environments.Live;
                
                _tradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(credentials.ApiKey, credentials.ApiSecret));
                _dataClient = environment.GetAlpacaDataClient(new SecretKey(credentials.ApiKey, credentials.ApiSecret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Alpaca clients");
            }
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    public async Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_tradingClient == null)
        {
            _logger.LogWarning("Alpaca trading client not initialized. Returning default account info.");
            return new AccountInfo
            {
                Cash = new Money(0, "USD"),
                Equity = new Money(0, "USD"),
                BuyingPower = new Money(0, "USD"),
                AccountNumber = string.Empty
            };
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);
            
            return new AccountInfo
            {
                Cash = new Money(account.TradableCash, "USD"),
                Equity = new Money(account.Equity ?? 0, "USD"),
                BuyingPower = new Money(account.BuyingPower ?? 0, "USD"),
                AccountNumber = account.AccountNumber ?? string.Empty
            };
        }, ServiceName, cancellationToken);
    }
    
    public async Task<List<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_tradingClient == null)
        {
            _logger.LogWarning("Alpaca trading client not initialized. Returning empty positions list.");
            return new List<Position>();
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var positions = await _tradingClient!.ListPositionsAsync(cancellationToken);
            
            return positions.Select(p => new Position
            {
                Symbol = new Symbol(p.Symbol),
                Quantity = new Quantity(p.Quantity),
                AveragePrice = new Money(p.AverageEntryPrice, "USD"),
                CurrentPrice = new Money(p.AssetCurrentPrice ?? 0m, "USD"),
                UnrealizedPnL = new Money(p.UnrealizedProfitLoss ?? 0m, "USD"),
                OpenedAt = DateTime.UtcNow, // IPosition doesn't have CreatedAtUtc or OpenTimeUtc in 7.0.0
                LastUpdatedAt = DateTime.UtcNow
            }).ToList();
        }, ServiceName, cancellationToken);
    }
    
    public async Task<List<Order>> GetOrdersAsync(Domain.Entities.OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_tradingClient == null)
        {
            _logger.LogWarning("Alpaca trading client not initialized. Returning empty orders list.");
            return new List<Order>();
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var request = new ListOrdersRequest
            {
                OrderStatusFilter = status.HasValue ? MapOrderStatus(status.Value) : OrderStatusFilter.All
            };
            
            if (from.HasValue)
            {
                request = new ListOrdersRequest
                {
                    OrderStatusFilter = status.HasValue ? MapOrderStatus(status.Value) : OrderStatusFilter.All
                };
                // TimeInterval is read-only, set it via constructor or use a different approach
            }
            
            var orders = await _tradingClient!.ListOrdersAsync(request, cancellationToken);
            
            return orders.Select(o => new Order
            {
                Id = o.OrderId.ToString(),
                ClientOrderId = o.ClientOrderId ?? o.OrderId.ToString(),
                Symbol = new Symbol(o.Symbol),
                Side = o.OrderSide == AlpacaOrderSide.Buy ? Domain.Entities.OrderSide.Buy : Domain.Entities.OrderSide.Sell,
                Type = MapOrderType(o.OrderType),
                Quantity = new Quantity((decimal)o.Quantity),
                LimitPrice = o.LimitPrice.HasValue ? new Money((decimal)o.LimitPrice.Value, "USD") : null,
                StopPrice = o.StopPrice != null ? new Money((decimal)o.StopPrice.Value, "USD") : null,
                Status = MapOrderStatus(o.OrderStatus),
                CreatedAt = o.CreatedAtUtc ?? DateTime.UtcNow,
                SubmittedAt = o.SubmittedAtUtc,
                FilledAt = o.FilledAtUtc,
                RejectionReason = string.Empty // IOrder doesn't have RejectReason in 7.0.0
            }).ToList();
        }, ServiceName, cancellationToken);
    }
    
    public async Task<Order> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_tradingClient == null)
        {
            _logger.LogWarning("Alpaca trading client not initialized. Cannot submit order.");
            throw new InvalidOperationException("Alpaca trading client not initialized. Please configure API credentials in Settings.");
        }
        
        return await _resilienceService.ExecuteAsync(async () =>
        {
            var orderSide = request.Side == Domain.Entities.OrderSide.Buy ? AlpacaOrderSide.Buy : AlpacaOrderSide.Sell;
            var orderType = MapOrderTypeToAlpaca(request.Type);
            
            IOrder order;
            
            var orderQuantity = OrderQuantity.Notional(request.Quantity.Value);
            
            if (request.Type == Domain.Entities.OrderType.Market)
            {
                var marketOrder = new NewOrderRequest(request.Symbol.Value, orderQuantity, orderSide, AlpacaOrderType.Market, TimeInForce.Day)
                {
                    ClientOrderId = request.ClientOrderId
                };
                order = await _tradingClient!.PostOrderAsync(marketOrder, cancellationToken);
            }
            else if (request.Type == Domain.Entities.OrderType.Limit && request.LimitPrice != null)
            {
                var limitOrder = new NewOrderRequest(request.Symbol.Value, orderQuantity, orderSide, AlpacaOrderType.Limit, TimeInForce.Day)
                {
                    ClientOrderId = request.ClientOrderId,
                    LimitPrice = request.LimitPrice.Amount
                };
                order = await _tradingClient!.PostOrderAsync(limitOrder, cancellationToken);
            }
            else if (request.Type == Domain.Entities.OrderType.Stop && request.StopPrice != null)
            {
                var stopOrder = new NewOrderRequest(request.Symbol.Value, orderQuantity, orderSide, AlpacaOrderType.Stop, TimeInForce.Day)
                {
                    ClientOrderId = request.ClientOrderId,
                    StopPrice = request.StopPrice.Amount
                };
                order = await _tradingClient!.PostOrderAsync(stopOrder, cancellationToken);
            }
            else if (request.Type == Domain.Entities.OrderType.StopLimit && request.LimitPrice != null && request.StopPrice != null)
            {
                var stopLimitOrder = new NewOrderRequest(request.Symbol.Value, orderQuantity, orderSide, AlpacaOrderType.StopLimit, TimeInForce.Day)
                {
                    ClientOrderId = request.ClientOrderId,
                    LimitPrice = request.LimitPrice.Amount,
                    StopPrice = request.StopPrice.Amount
                };
                order = await _tradingClient!.PostOrderAsync(stopLimitOrder, cancellationToken);
            }
            else
            {
                throw new ArgumentException($"Invalid order type or missing price: {request.Type}");
            }
            
            return new Order
            {
                Id = order.OrderId.ToString(),
                ClientOrderId = order.ClientOrderId ?? order.OrderId.ToString(),
                Symbol = new Symbol(order.Symbol),
                Side = order.OrderSide == AlpacaOrderSide.Buy ? Domain.Entities.OrderSide.Buy : Domain.Entities.OrderSide.Sell,
                Type = MapOrderType(order.OrderType),
                Quantity = new Quantity((decimal)order.Quantity),
                LimitPrice = order.LimitPrice != null ? new Money((decimal)order.LimitPrice.Value, "USD") : null,
                StopPrice = order.StopPrice != null ? new Money((decimal)order.StopPrice.Value, "USD") : null,
                Status = MapOrderStatus(order.OrderStatus),
                CreatedAt = order.CreatedAtUtc ?? DateTime.UtcNow,
                SubmittedAt = order.SubmittedAtUtc
            };
        }, ServiceName, cancellationToken);
    }
    
    public async Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        if (_tradingClient == null)
        {
            _logger.LogWarning("Alpaca trading client not initialized. Cannot cancel order.");
            throw new InvalidOperationException("Alpaca trading client not initialized. Please configure API credentials in Settings.");
        }
        
        await _resilienceService.ExecuteAsync(async () =>
        {
            if (Guid.TryParse(orderId, out var guid))
            {
                await _tradingClient!.CancelOrderAsync(guid, cancellationToken);
            }
            else
            {
                throw new ArgumentException($"Invalid order ID format: {orderId}");
            }
        }, ServiceName, cancellationToken);
    }
    
    private static OrderStatusFilter MapOrderStatus(Domain.Entities.OrderStatus status)
    {
        return status switch
        {
            Domain.Entities.OrderStatus.Pending => OrderStatusFilter.Open,
            Domain.Entities.OrderStatus.Submitted => OrderStatusFilter.Open,
            Domain.Entities.OrderStatus.PartiallyFilled => OrderStatusFilter.Open,
            Domain.Entities.OrderStatus.Filled => OrderStatusFilter.Closed,
            Domain.Entities.OrderStatus.Canceled => OrderStatusFilter.Closed,
            Domain.Entities.OrderStatus.Rejected => OrderStatusFilter.Closed,
            _ => OrderStatusFilter.All
        };
    }
    
    private static Domain.Entities.OrderStatus MapOrderStatus(AlpacaOrderStatus status)
    {
        return status switch
        {
            AlpacaOrderStatus.New => Domain.Entities.OrderStatus.Submitted,
            AlpacaOrderStatus.Accepted => Domain.Entities.OrderStatus.Submitted,
            AlpacaOrderStatus.PendingNew => Domain.Entities.OrderStatus.Pending,
            AlpacaOrderStatus.PartiallyFilled => Domain.Entities.OrderStatus.PartiallyFilled,
            AlpacaOrderStatus.Filled => Domain.Entities.OrderStatus.Filled,
            AlpacaOrderStatus.DoneForDay => Domain.Entities.OrderStatus.Canceled,
            AlpacaOrderStatus.Canceled => Domain.Entities.OrderStatus.Canceled,
            AlpacaOrderStatus.PendingCancel => Domain.Entities.OrderStatus.Canceled,
            AlpacaOrderStatus.PendingReplace => Domain.Entities.OrderStatus.Submitted,
            AlpacaOrderStatus.Replaced => Domain.Entities.OrderStatus.Submitted,
            AlpacaOrderStatus.Rejected => Domain.Entities.OrderStatus.Rejected,
            AlpacaOrderStatus.Expired => Domain.Entities.OrderStatus.Canceled,
            _ => Domain.Entities.OrderStatus.Pending
        };
    }
    
    private static AlpacaOrderType MapOrderTypeToAlpaca(Domain.Entities.OrderType type)
    {
        return type switch
        {
            Domain.Entities.OrderType.Market => AlpacaOrderType.Market,
            Domain.Entities.OrderType.Limit => AlpacaOrderType.Limit,
            Domain.Entities.OrderType.Stop => AlpacaOrderType.Stop,
            Domain.Entities.OrderType.StopLimit => AlpacaOrderType.StopLimit,
            _ => AlpacaOrderType.Market
        };
    }
    
    private static Domain.Entities.OrderType MapOrderType(AlpacaOrderType type)
    {
        return type switch
        {
            AlpacaOrderType.Market => Domain.Entities.OrderType.Market,
            AlpacaOrderType.Limit => Domain.Entities.OrderType.Limit,
            AlpacaOrderType.Stop => Domain.Entities.OrderType.Stop,
            AlpacaOrderType.StopLimit => Domain.Entities.OrderType.StopLimit,
            _ => Domain.Entities.OrderType.Market
        };
    }
    
    public void Dispose()
    {
        _tradingClient?.Dispose();
        _dataClient?.Dispose();
    }
}
