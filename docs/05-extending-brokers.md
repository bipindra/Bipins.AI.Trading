# Extending Brokers

## Adding a New Broker

The platform is designed to support multiple brokers through the port/adapter pattern.

## Implementation Steps

### 1. Implement IBrokerClient

Create a new class implementing `IBrokerClient`:

```csharp
public class YourBrokerClient : IBrokerClient
{
    public Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        // Implement account retrieval
    }
    
    public Task<List<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        // Implement position retrieval
    }
    
    public Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        // Implement order retrieval
    }
    
    public Task<Order> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        // Implement order submission
    }
    
    public Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        // Implement order cancellation
    }
}
```

### 2. Implement IMarketDataClient

Create a new class implementing `IMarketDataClient`:

```csharp
public class YourMarketDataClient : IMarketDataClient
{
    public Task<List<Candle>> GetHistoricalCandlesAsync(
        Symbol symbol,
        Timeframe timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        // Implement historical data retrieval
    }
    
    public Task<List<Candle>> PollLatestCandlesAsync(
        List<Symbol> symbols,
        Timeframe timeframe,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        // Implement latest data polling
    }
    
    public Task<decimal> GetCurrentPriceAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        // Implement current price retrieval
    }
}
```

### 3. Add Configuration Options

Add to `BrokerOptions`:

```csharp
public class BrokerOptions
{
    public string Provider { get; set; } = "Alpaca";
    public AlpacaOptions Alpaca { get; set; } = new();
    public YourBrokerOptions YourBroker { get; set; } = new(); // Add this
}

public class YourBrokerOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.yourbroker.com";
}
```

### 4. Register in DependencyInjection

Update `Infrastructure/DependencyInjection.cs`:

```csharp
var brokerOptions = configuration.GetSection(BrokerOptions.SectionName).Get<BrokerOptions>();
if (brokerOptions?.Provider == "YourBroker")
{
    services.AddSingleton<IBrokerClient, YourBrokerClient>();
    services.AddSingleton<IMarketDataClient, YourMarketDataClient>();
}
else if (brokerOptions?.Provider == "Alpaca")
{
    // Existing Alpaca registration
}
```

### 5. Update appsettings.json

```json
{
  "Broker": {
    "Provider": "YourBroker",
    "YourBroker": {
      "ApiKey": "YOUR_KEY",
      "ApiSecret": "YOUR_SECRET",
      "BaseUrl": "https://api.yourbroker.com"
    }
  }
}
```

## Mapping Considerations

### Order Types

Map broker-specific order types to domain types:
- Market → `OrderType.Market`
- Limit → `OrderType.Limit`
- Stop → `OrderType.Stop`
- StopLimit → `OrderType.StopLimit`

### Order Status

Map broker statuses to domain statuses:
- Pending/New → `OrderStatus.Pending`
- Submitted/Accepted → `OrderStatus.Submitted`
- PartiallyFilled → `OrderStatus.PartiallyFilled`
- Filled → `OrderStatus.Filled`
- Canceled → `OrderStatus.Canceled`
- Rejected → `OrderStatus.Rejected`

### Timeframes

Map broker timeframes to domain timeframes:
- 1m, 5m, 15m, 1h, 1d → `Timeframe` value objects

## Error Handling

Handle broker-specific errors:

```csharp
try
{
    // Broker API call
}
catch (BrokerSpecificException ex)
{
    _logger.LogError(ex, "Broker-specific error");
    throw new DomainException("User-friendly message", ex);
}
```

## Testing

1. Create unit tests with mock broker responses
2. Test all IBrokerClient methods
3. Test all IMarketDataClient methods
4. Test error scenarios
5. Test mapping correctness

## Example: Interactive Brokers

See `AlpacaBrokerClient.cs` and `AlpacaMarketDataClient.cs` for reference implementation.

Key patterns:
- Configuration via IOptions
- Logging for all operations
- Error handling and retries
- Mapping to domain types
- Disposal pattern for resources
