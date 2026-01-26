# Live Stock Price Charts Implementation

## Overview
Add interactive stock price charts similar to TradingView to visualize real-time and historical price movements. This will include candlestick charts, technical indicators overlay, and real-time updates.

## Goals
- Display candlestick charts for selected symbols
- Show real-time price updates
- Overlay technical indicators (MACD, RSI, etc.)
- Interactive chart with zoom, pan, and time range selection
- Support multiple timeframes (1m, 5m, 15m, 1h, 1d)
- Display current positions and orders on chart

## Files to Create/Update

### 1. Create Chart API Controller
- **File**: `src/Bipins.AI.Trading.Web/Controllers/ChartsController.cs`
- **Purpose**: REST API endpoints for chart data
- **Endpoints**:
  - `GET /api/charts/candles?symbol={symbol}&timeframe={timeframe}&from={from}&to={to}` - Get historical candles
  - `GET /api/charts/indicators?symbol={symbol}&timeframe={timeframe}&indicator={indicator}` - Get indicator data
  - `GET /api/charts/latest?symbol={symbol}` - Get latest price update

### 2. Create Chart View Page
- **File**: `src/Bipins.AI.Trading.Web/Controllers/ChartsController.cs` (MVC action)
- **File**: `src/Bipins.AI.Trading.Web/Views/Charts/Index.cshtml`
- **Purpose**: Main chart page with symbol selector and timeframe controls

### 3. Add Chart JavaScript Library
- **Option 1**: TradingView Lightweight Charts (recommended - free, fast, TradingView-like)
- **Option 2**: Chart.js with candlestick plugin
- **Option 3**: ApexCharts
- **Decision**: Use TradingView Lightweight Charts for best TradingView-like experience

### 4. Create Chart JavaScript Module
- **File**: `src/Bipins.AI.Trading.Web/wwwroot/js/charts.js`
- **Purpose**: Chart initialization, data loading, real-time updates
- **Features**:
  - Initialize TradingView chart
  - Load historical data via API
  - Update chart with new candles
  - Overlay indicators
  - Handle symbol/timeframe changes

### 5. Add SignalR Hub for Real-Time Updates (Optional)
- **File**: `src/Bipins.AI.Trading.Web/Hubs/ChartHub.cs`
- **Purpose**: Push real-time price updates to connected clients
- **Methods**:
  - `SubscribeToSymbol(string symbol)` - Subscribe to symbol updates
  - `UnsubscribeFromSymbol(string symbol)` - Unsubscribe

### 6. Update Market Data Service
- **File**: `src/Bipins.AI.Trading.Web/Services/MarketDataIngestionHostedService.cs`
- **Changes**: Emit SignalR events when new candles arrive (if using SignalR)

### 7. Create Chart ViewModel
- **File**: `src/Bipins.AI.Trading.Web/ViewModels/ChartViewModel.cs`
- **Properties**:
  - AvailableSymbols
  - SelectedSymbol
  - AvailableTimeframes
  - SelectedTimeframe
  - DefaultTimeRange

### 8. Update Navigation
- **File**: `src/Bipins.AI.Trading.Web/Views/Shared/_Layout.cshtml`
- **Changes**: Add "Charts" link to navigation menu

### 9. Add Chart CSS
- **File**: `src/Bipins.AI.Trading.Web/wwwroot/css/charts.css`
- **Purpose**: Styling for chart container and controls

### 10. Create Chart API DTOs
- **File**: `src/Bipins.AI.Trading.Web/Models/ChartModels.cs`
- **Classes**:
  - `CandleDataDto` - Candle data for API response
  - `IndicatorDataDto` - Indicator values for API response
  - `ChartDataResponse` - Combined response with candles and indicators

## Implementation Details

### Chart Library: TradingView Lightweight Charts

**Why TradingView Lightweight Charts:**
- Free and open source
- High performance
- TradingView-like appearance
- Built-in candlestick support
- Easy to integrate
- Supports indicators overlay

**Installation:**
- Add via CDN or npm package
- Include in `_Layout.cshtml` or chart page

### API Endpoints

#### GET /api/charts/candles
```json
{
  "symbol": "SPY",
  "timeframe": "5m",
  "from": "2024-01-01T00:00:00Z",
  "to": "2024-01-02T00:00:00Z"
}
```

**Response:**
```json
{
  "candles": [
    {
      "time": 1704067200,
      "open": 450.25,
      "high": 451.50,
      "low": 449.80,
      "close": 451.00,
      "volume": 1234567
    }
  ],
  "symbol": "SPY",
  "timeframe": "5m"
}
```

#### GET /api/charts/indicators
```json
{
  "symbol": "SPY",
  "timeframe": "5m",
  "indicator": "MACD"
}
```

**Response:**
```json
{
  "indicator": "MACD",
  "data": [
    {
      "time": 1704067200,
      "value": 0.25,
      "signal": 0.20,
      "histogram": 0.05
    }
  ]
}
```

### Chart Features

1. **Candlestick Chart**:
   - Green/red candles for up/down
   - Volume bars below
   - Time axis with automatic scaling

2. **Indicators Overlay**:
   - MACD (below chart)
   - RSI (separate pane)
   - Moving averages (on chart)
   - Bollinger Bands (on chart)

3. **Interactive Controls**:
   - Symbol selector dropdown
   - Timeframe selector (1m, 5m, 15m, 1h, 1d)
   - Time range selector (1D, 1W, 1M, 3M, 1Y, All)
   - Zoom in/out
   - Pan left/right
   - Crosshair for price/time display

4. **Real-Time Updates**:
   - Poll API every 5-10 seconds for latest candle
   - Or use SignalR for push updates
   - Smooth animation when new data arrives

5. **Position/Order Markers**:
   - Show open positions on chart
   - Show pending orders
   - Show filled orders
   - Color-coded markers

### Chart JavaScript Structure

```javascript
// Initialize chart
const chart = LightweightCharts.createChart(container, {
  width: container.clientWidth,
  height: 600,
  layout: { backgroundColor: '#ffffff', textColor: '#333' },
  grid: { vertLines: { color: '#f0f0f0' }, horzLines: { color: '#f0f0f0' } },
  timeScale: { timeVisible: true, secondsVisible: false }
});

// Add candlestick series
const candlestickSeries = chart.addCandlestickSeries({
  upColor: '#26a69a',
  downColor: '#ef5350',
  borderVisible: false,
  wickUpColor: '#26a69a',
  wickDownColor: '#ef5350'
});

// Load historical data
async function loadChartData(symbol, timeframe, from, to) {
  const response = await fetch(`/api/charts/candles?symbol=${symbol}&timeframe=${timeframe}&from=${from}&to=${to}`);
  const data = await response.json();
  candlestickSeries.setData(data.candles.map(c => ({
    time: c.time,
    open: c.open,
    high: c.high,
    low: c.low,
    close: c.close
  })));
}

// Real-time updates
setInterval(async () => {
  const latest = await fetch(`/api/charts/latest?symbol=${symbol}`);
  const candle = await latest.json();
  candlestickSeries.update(candle);
}, 5000);
```

### Real-Time Updates Strategy

**Option 1: Polling (Simpler)**
- Poll `/api/charts/latest` every 5-10 seconds
- Update chart with new candle if timestamp changed
- Pros: Simple, no additional infrastructure
- Cons: Slight delay, more HTTP requests

**Option 2: SignalR (Better UX)**
- Push updates when new candles arrive
- Real-time, low latency
- Pros: Real-time, efficient
- Cons: Requires SignalR setup

**Recommendation**: Start with polling, add SignalR later if needed.

### Chart Page Layout

```
┌─────────────────────────────────────────┐
│  Charts                          [Symbol▼] [5m▼] [1D▼] │
├─────────────────────────────────────────┤
│                                           │
│         [TradingView Chart]              │
│                                           │
│  [MACD Indicator]                        │
│  [RSI Indicator]                         │
│                                           │
├─────────────────────────────────────────┤
│  Indicators: [MACD] [RSI] [MA] [BB]      │
│  Positions: [Show/Hide]                  │
│  Orders: [Show/Hide]                     │
└─────────────────────────────────────────┘
```

## Dependencies

### NuGet Packages
- None required (using TradingView Lightweight Charts via CDN)

### JavaScript Libraries (CDN)
- TradingView Lightweight Charts: `https://unpkg.com/lightweight-charts/dist/lightweight-charts.standalone.production.js`

### Optional
- SignalR (if implementing real-time updates)

## Configuration

Add to `appsettings.json`:
```json
{
  "Charts": {
    "DefaultSymbol": "SPY",
    "DefaultTimeframe": "5m",
    "DefaultTimeRange": "1D",
    "UpdateIntervalSeconds": 5,
    "MaxCandles": 1000
  }
}
```

## Testing Strategy

- Test chart loads with different symbols
- Test timeframe switching
- Test time range selection
- Test real-time updates
- Test with no data available
- Test with large datasets (performance)

## Success Criteria

- Chart displays candlestick data correctly
- Real-time updates work (polling or SignalR)
- Indicators can be toggled on/off
- Symbol and timeframe can be changed
- Chart is responsive and performs well
- Positions and orders visible on chart (optional)
- Works on different screen sizes

## Future Enhancements

- Add more indicators (Stochastic, Bollinger Bands, etc.)
- Add drawing tools (trend lines, support/resistance)
- Add alerts (price alerts, indicator crossovers)
- Add multiple symbol comparison
- Add volume profile
- Add order book visualization
- Export chart as image
- Save chart layouts
