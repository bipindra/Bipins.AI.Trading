# Extending Strategies

## Adding a New Decision Engine

The platform supports multiple decision engines through the `IDecisionEngine` interface.

## Implementation Steps

### 1. Implement IDecisionEngine

Create a new class implementing `IDecisionEngine`:

```csharp
public class YourStrategyEngine : IDecisionEngine
{
    private readonly ILogger<YourStrategyEngine> _logger;
    
    public YourStrategyEngine(ILogger<YourStrategyEngine> logger)
    {
        _logger = logger;
    }
    
    public async Task<TradeDecision> MakeDecisionAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, decimal> features,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        // Your strategy logic here
        
        return new TradeDecision
        {
            Symbol = symbol,
            Timeframe = timeframe,
            CandleTimestamp = candles.Last().Timestamp,
            Action = TradeAction.Buy, // or Sell, Hold
            QuantityPercent = 5.0m, // % of portfolio
            Confidence = 0.75m, // 0-1
            Rationale = "Your reasoning",
            Features = features
        };
    }
}
```

### 2. Register in DependencyInjection

Update `Application/DependencyInjection.cs`:

```csharp
// Option 1: Replace default
services.AddScoped<IDecisionEngine, YourStrategyEngine>();

// Option 2: Factory pattern for multiple strategies
services.AddScoped<IDecisionEngine>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var strategy = config["Trading:Strategy"] ?? "Default";
    
    return strategy switch
    {
        "YourStrategy" => sp.GetRequiredService<YourStrategyEngine>(),
        _ => sp.GetRequiredService<DecisionEngine>()
    };
});
```

### 3. Strategy Configuration

Add to `TradingOptions`:

```csharp
public class TradingOptions
{
    // ... existing properties
    public string Strategy { get; set; } = "Default";
}
```

Update `appsettings.json`:

```json
{
  "Trading": {
    "Strategy": "YourStrategy"
  }
}
```

## Strategy Examples

### Mean Reversion Strategy

```csharp
public class MeanReversionEngine : IDecisionEngine
{
    public async Task<TradeDecision> MakeDecisionAsync(...)
    {
        var prices = candles.Select(c => c.Close).ToList();
        var mean = prices.Average();
        var stdDev = CalculateStdDev(prices);
        var latest = candles.Last();
        
        var zScore = (latest.Close - mean) / stdDev;
        
        TradeAction action = TradeAction.Hold;
        decimal confidence = 0.5m;
        
        if (zScore < -2) // 2 std devs below mean
        {
            action = TradeAction.Buy;
            confidence = 0.8m;
        }
        else if (zScore > 2) // 2 std devs above mean
        {
            action = TradeAction.Sell;
            confidence = 0.8m;
        }
        
        return new TradeDecision
        {
            // ... set properties
            Action = action,
            Confidence = confidence,
            Rationale = $"Z-Score: {zScore:F2}"
        };
    }
}
```

### AI/LLM Strategy (Future)

```csharp
public class AIDecisionEngine : IDecisionEngine
{
    private readonly IVectorMemoryStore _vectorStore;
    private readonly ILLMService _llmService; // Hypothetical
    
    public async Task<TradeDecision> MakeDecisionAsync(...)
    {
        // 1. Convert current setup to embedding
        var embedding = await _llmService.GetEmbeddingAsync(candles, features);
        
        // 2. Search similar past decisions
        var similar = await _vectorStore.SearchAsync(
            "trading_decisions",
            embedding,
            topK: 5
        );
        
        // 3. Build context from similar decisions
        var context = BuildContext(similar);
        
        // 4. Call LLM for decision
        var decision = await _llmService.MakeDecisionAsync(
            symbol, candles, features, context
        );
        
        // 5. Store decision in vector store
        await _vectorStore.UpsertAsync(
            "trading_decisions",
            new VectorDocument
            {
                Id = decision.Id,
                Vector = embedding,
                Payload = new Dictionary<string, object>
                {
                    ["Symbol"] = symbol.Value,
                    ["Action"] = decision.Action.ToString(),
                    ["Outcome"] = "Pending" // Updated later
                }
            }
        );
        
        return decision;
    }
}
```

## Using Features

Features are computed by `FeatureComputeHostedService` and passed to the decision engine:

```csharp
// Available in features dictionary:
features["PriceChange"] // Price change from previous candle
features["PriceChangePercent"] // Percentage change
features["VolumeChange"] // Volume change
features["Range"] // High - Low
features["Body"] // |Close - Open|
features["CloseToHighRatio"] // Close / High
features["CloseToLowRatio"] // Close / Low

// Add custom features in FeatureComputeHostedService
```

## Portfolio Context

The portfolio is provided for position-aware decisions:

```csharp
// Check existing positions
var existingPosition = portfolio.GetPosition(symbol);
if (existingPosition != null && !existingPosition.IsFlat)
{
    // Adjust decision based on existing position
}

// Check portfolio constraints
if (portfolio.Cash.Amount < minCash)
{
    // Don't open new positions
}
```

## Testing Strategies

1. **Unit Tests**: Test decision logic with mock data
2. **Backtesting**: Run against historical data
3. **Paper Trading**: Test in Ask mode first
4. **Metrics**: Track decision quality, win rate, etc.

## Best Practices

1. **Start Simple**: Begin with basic indicators
2. **Add Features Gradually**: Don't over-engineer
3. **Test Thoroughly**: Use Ask mode extensively
4. **Monitor Performance**: Track decision outcomes
5. **Document Logic**: Explain rationale in code
6. **Version Control**: Track strategy changes

## Integration with Vector Store

For AI strategies, use Qdrant to:
- Store past decisions with outcomes
- Retrieve similar market setups
- Learn from past performance
- Build context for LLM decisions

See `IVectorMemoryStore` interface for API details.
