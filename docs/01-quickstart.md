# Quick Start Guide

## Prerequisites

- .NET 8 SDK
- Docker Desktop (for Qdrant)
- Alpaca paper trading account (optional, for testing)

## Setup Steps

### 1. Clone and Build

```bash
cd Bipins.AI.Trading
dotnet restore
dotnet build
```

### 2. Start Qdrant

```bash
cd docker
docker-compose up -d
```

Verify Qdrant is running:
```bash
curl http://localhost:6333/health
```

### 3. Configure Application

Edit `src/Bipins.AI.Trading.Web/appsettings.json`:

```json
{
  "Broker": {
    "Alpaca": {
      "ApiKey": "YOUR_ALPACA_API_KEY",
      "ApiSecret": "YOUR_ALPACA_SECRET",
      "BaseUrl": "https://paper-api.alpaca.markets"
    }
  },
  "Trading": {
    "Enabled": false,
    "Mode": "Ask",
    "Symbols": ["SPY"],
    "Timeframe": "5m"
  }
}
```

### 4. Run Application

```bash
cd src/Bipins.AI.Trading.Web
dotnet run
```

### 5. Access Portal

Navigate to: `https://localhost:5001`

Default credentials:
- Username: `admin`
- Password: `admin`

## First Run Checklist

1. ✅ Qdrant is running
2. ✅ Alpaca credentials configured (or using NoOp broker)
3. ✅ Trading.Enabled = false (safety first!)
4. ✅ Application starts without errors
5. ✅ Can login to portal
6. ✅ Dashboard shows portfolio (may be empty initially)

## Testing Without Alpaca

If you don't have Alpaca credentials, the system will use NoOp implementations:
- Broker returns mock data
- Market data returns empty lists
- Orders will fail (expected)

This allows you to test the portal and event flow without a broker connection.

## Next Steps

1. Review configuration (see `02-configuration.md`)
2. Understand architecture (see `03-architecture.md`)
3. Learn operations (see `04-operations.md`)
