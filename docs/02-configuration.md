# Configuration Reference

## Configuration Sources

Configuration is loaded from:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables (override JSON)

## Broker Configuration

### Alpaca

```json
{
  "Broker": {
    "Provider": "Alpaca",
    "Alpaca": {
      "ApiKey": "YOUR_API_KEY",
      "ApiSecret": "YOUR_SECRET",
      "BaseUrl": "https://paper-api.alpaca.markets"
    }
  }
}
```

**Environment Variables:**
- `Broker__Alpaca__ApiKey`
- `Broker__Alpaca__ApiSecret`
- `Broker__Alpaca__BaseUrl`

## Trading Configuration

```json
{
  "Trading": {
    "Enabled": false,
    "Mode": "Ask",
    "Symbols": ["SPY", "QQQ"],
    "Timeframe": "5m",
    "MaxOrdersPerCandle": 1,
    "CooldownSeconds": 60
  }
}
```

**Options:**
- `Enabled`: Must be `true` for trading to execute (default: `false`)
- `Mode`: `Ask` (manual approval) or `Auto` (automatic)
- `Symbols`: Array of symbols to trade
- `Timeframe`: `1m`, `5m`, `15m`, `1h`, `1d`
- `MaxOrdersPerCandle`: Maximum orders per candle period
- `CooldownSeconds`: Minimum seconds between decisions

**Environment Variables:**
- `Trading__Enabled`
- `Trading__Mode`
- `Trading__Symbols__0` (for first symbol)
- `Trading__Timeframe`

## Risk Configuration

```json
{
  "Risk": {
    "MaxPositionPercent": 10.0,
    "MaxOpenPositions": 5,
    "MaxDailyLossPercent": 5.0,
    "AtrLimit": 2.0,
    "SpreadLimit": 0.01
  }
}
```

**Options:**
- `MaxPositionPercent`: Maximum position size as % of equity
- `MaxOpenPositions`: Maximum number of open positions
- `MaxDailyLossPercent`: Maximum daily loss as % of equity
- `AtrLimit`: ATR multiplier for stop loss (not fully implemented in v1)
- `SpreadLimit`: Maximum spread for order execution (not fully implemented in v1)

## Storage Configuration

```json
{
  "Storage": {
    "DbProvider": "InMemorySqlite",
    "ConnectionString": "Data Source=:memory:"
  }
}
```

**For SQL Server (future):**
```json
{
  "Storage": {
    "DbProvider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=Trading;Integrated Security=true"
  }
}
```

## Cache Configuration

```json
{
  "Cache": {
    "Provider": "Memory"
  }
}
```

**For Redis (future):**
```json
{
  "Cache": {
    "Provider": "Redis",
    "RedisConnectionString": "localhost:6379"
  }
}
```

## Vector Database Configuration

```json
{
  "VectorDb": {
    "Provider": "Qdrant",
    "Qdrant": {
      "Endpoint": "http://localhost:6333",
      "CollectionName": "trading_decisions"
    }
  }
}
```

## Logging Configuration

Serilog is configured in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/trading-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Safe Defaults

All configurations default to safe values:
- Trading disabled
- Ask mode (requires approval)
- Conservative risk limits
- In-memory storage (no persistence risk)

## Configuration Validation

The application validates configuration on startup. Check logs for:
- Missing Alpaca credentials (will use NoOp)
- Invalid timeframes
- Missing symbols
