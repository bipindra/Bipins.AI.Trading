# Architecture Documentation

## Clean Architecture Layers

### Domain Layer
- **Entities**: Candle, Position, Order, Fill, Portfolio, TradeDecision
- **Value Objects**: Symbol, Money, Quantity, Timeframe
- **Events**: CandleReceived, PositionChanged, RiskBreachDetected
- **No Dependencies**: Pure business logic

### Application Layer
- **Ports (Interfaces)**: IBrokerClient, IMarketDataClient, IRiskManager, IDecisionEngine
- **Contracts (Messages)**: CandleClosed, TradeProposed, TradeApproved, etc.
- **Services**: RiskManager, DecisionEngine, PortfolioService
- **Consumers**: MassTransit message consumers
- **Depends on**: Domain only

### Infrastructure Layer
- **Broker Adapters**: AlpacaBrokerClient, AlpacaMarketDataClient
- **Persistence**: EF Core, Repositories, SQLite
- **Cache**: MemoryCacheStore (Redis-ready)
- **Vector**: QdrantVectorStore
- **Depends on**: Application + Domain

### Web Layer
- **Controllers**: MVC controllers for portal
- **Views**: Razor views with Bootstrap
- **Hosted Services**: Background services for trading loop
- **Depends on**: All layers

## Event Flow

```
MarketDataIngestion → CandleClosed
    ↓
FeatureCompute → FeaturesComputed
    ↓
TradingDecision → TradeProposed
    ↓
RiskManager → TradeApproved / TradeRejected
    ↓ (if Ask mode)
ActionsController → User Approval
    ↓
TradeApprovedConsumer → OrderSubmitted
    ↓
Broker → OrderFilled
    ↓
PortfolioReconciliation → PortfolioUpdated
```

## Message Contracts

All messages implement MassTransit contracts:

- **CandleClosed**: New candle received
- **FeaturesComputed**: Technical indicators calculated
- **TradeProposed**: Decision engine output
- **TradeApproved**: Risk check passed + approved
- **TradeRejected**: Risk check failed
- **ActionRequired**: Ask mode - waiting approval
- **OrderSubmitted**: Order sent to broker
- **OrderFilled**: Order executed
- **PortfolioUpdated**: Portfolio state changed
- **RiskBreach**: Risk limit exceeded
- **FeedDisconnected**: Market data connection lost

## Data Flow

### Candle Ingestion
1. MarketDataIngestionHostedService polls Alpaca
2. Publishes CandleClosed
3. CandleClosedConsumer stores candle
4. FeatureComputeHostedService computes features
5. Publishes FeaturesComputed

### Decision Making
1. TradingDecisionHostedService reads features
2. Calls IDecisionEngine.MakeDecisionAsync
3. Publishes TradeProposed
4. TradeProposedConsumer checks risk
5. Publishes TradeApproved or TradeRejected

### Order Execution
1. TradeApprovedConsumer receives approval
2. Calculates order quantity
3. Calls IBrokerClient.SubmitOrderAsync
4. Publishes OrderSubmitted
5. Broker fills order
6. OrderFilledConsumer updates portfolio

## Idempotency

All operations are idempotent:
- **Candles**: Key = `{Symbol}_{Timeframe}_{Timestamp}`
- **Decisions**: Key = `{Symbol}_{Timeframe}_{CandleTimestamp}`
- **Orders**: Key = `ClientOrderId`

## Error Handling

- All broker operations wrapped in try-catch
- Failed operations logged with correlation ID
- Consumers handle exceptions gracefully
- No-op implementations for missing dependencies

## Scalability Considerations

- **Message Bus**: In-memory now, switch to RabbitMQ/Azure Service Bus
- **Database**: SQLite in-memory now, switch to SQL Server
- **Cache**: IMemoryCache now, switch to Redis
- **Vector DB**: Qdrant (already external)

## Testing Strategy

- **Unit Tests**: Domain logic, services, risk manager
- **Integration Tests**: Consumers, repositories, EF Core
- **Fake Broker**: NoOpBrokerClient for testing without Alpaca
