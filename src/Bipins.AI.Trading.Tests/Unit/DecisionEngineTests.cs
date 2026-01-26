using Bipins.AI.Trading.Application.Services;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class DecisionEngineTests
{
    private readonly DecisionEngine _decisionEngine;
    
    public DecisionEngineTests()
    {
        var loggerMock = new Mock<ILogger<DecisionEngine>>();
        _decisionEngine = new DecisionEngine(loggerMock.Object);
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldReturnHold_WhenInsufficientData()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandles(10); // Less than 14 required
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio();
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Action.Should().Be(TradeAction.Hold);
        decision.Confidence.Should().Be(0.0m);
        decision.Rationale.Should().Contain("Insufficient historical data");
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldReturnBuy_WhenRSIOversold()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandlesWithRSI(20, 25.0m); // RSI < 30 (oversold)
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD")
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Action.Should().Be(TradeAction.Buy);
        decision.Confidence.Should().BeGreaterThan(0.5m);
        decision.Rationale.Should().Contain("RSI oversold");
        decision.QuantityPercent.Should().HaveValue();
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldReturnSell_WhenRSIOverbought()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandlesWithRSI(20, 75.0m); // RSI > 70 (overbought)
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD")
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Action.Should().Be(TradeAction.Sell);
        decision.Confidence.Should().BeGreaterThan(0.5m);
        decision.Rationale.Should().Contain("RSI overbought");
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldReturnHold_WhenAlreadyLong()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandlesWithRSI(20, 25.0m);
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            Positions = new List<Position>
            {
                new Position
                {
                    Symbol = symbol,
                    Quantity = new Quantity(100), // Already long
                    AveragePrice = new Money(400, "USD"),
                    CurrentPrice = new Money(400, "USD")
                }
            }
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Action.Should().Be(TradeAction.Hold);
        decision.Rationale.Should().Contain("Already long");
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldIncludeStopLoss_WhenBuyDecision()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandlesWithRSI(20, 25.0m);
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD")
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        if (decision.Action == TradeAction.Buy)
        {
            decision.SuggestedStopLoss.Should().NotBeNull();
            decision.SuggestedTakeProfit.Should().NotBeNull();
            decision.SuggestedStopLoss!.Amount.Should().BeLessThan(candles.Last().Close);
            decision.SuggestedTakeProfit!.Amount.Should().BeGreaterThan(candles.Last().Close);
        }
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldIncludeFeatures()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandles(20);
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD")
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Features.Should().ContainKey("RSI");
        decision.Features.Should().ContainKey("MACD");
        decision.Features.Should().ContainKey("Close");
    }
    
    [Fact]
    public async Task MakeDecisionAsync_ShouldHaveValidStructure()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        var timeframe = new Timeframe("5m");
        var candles = GenerateCandles(20);
        var features = new Dictionary<string, decimal>();
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD")
        };
        
        // Act
        var decision = await _decisionEngine.MakeDecisionAsync(
            symbol, timeframe, candles, features, portfolio);
        
        // Assert
        decision.Symbol.Should().Be(symbol);
        decision.Timeframe.Should().Be(timeframe);
        decision.CandleTimestamp.Should().Be(candles.Last().Timestamp);
        decision.Confidence.Should().BeInRange(0m, 1m);
        decision.Rationale.Should().NotBeNullOrEmpty();
        decision.Id.Should().NotBeNullOrEmpty();
    }
    
    private List<Candle> GenerateCandles(int count)
    {
        var candles = new List<Candle>();
        var basePrice = 400m;
        var timestamp = DateTime.UtcNow.AddMinutes(-count * 5);
        
        for (int i = 0; i < count; i++)
        {
            var change = (decimal)(Random.Shared.NextDouble() - 0.5) * 2;
            var open = basePrice + change;
            var close = open + (decimal)(Random.Shared.NextDouble() - 0.5) * 1;
            var high = Math.Max(open, close) + (decimal)Random.Shared.NextDouble() * 0.5m;
            var low = Math.Min(open, close) - (decimal)Random.Shared.NextDouble() * 0.5m;
            
            candles.Add(new Candle
            {
                Symbol = new Symbol("SPY"),
                Timeframe = new Timeframe("5m"),
                Timestamp = timestamp.AddMinutes(i * 5),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = Random.Shared.NextInt64(1000000, 10000000)
            });
            
            basePrice = close;
        }
        
        return candles;
    }
    
    private List<Candle> GenerateCandlesWithRSI(int count, decimal targetRSI)
    {
        // Generate candles that will result in target RSI
        var candles = new List<Candle>();
        var basePrice = 400m;
        var timestamp = DateTime.UtcNow.AddMinutes(-count * 5);
        
        // For oversold (low RSI), generate mostly down candles
        // For overbought (high RSI), generate mostly up candles
        var trend = targetRSI < 50 ? -1m : 1m;
        var volatility = Math.Abs(targetRSI - 50) / 50;
        
        for (int i = 0; i < count; i++)
        {
            var change = trend * volatility * (decimal)(Random.Shared.NextDouble() + 0.5) * 2;
            var open = basePrice;
            var close = open + change;
            var high = Math.Max(open, close) + (decimal)Random.Shared.NextDouble() * 0.5m;
            var low = Math.Min(open, close) - (decimal)Random.Shared.NextDouble() * 0.5m;
            
            candles.Add(new Candle
            {
                Symbol = new Symbol("SPY"),
                Timeframe = new Timeframe("5m"),
                Timestamp = timestamp.AddMinutes(i * 5),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = Random.Shared.NextInt64(1000000, 10000000)
            });
            
            basePrice = close;
        }
        
        return candles;
    }
}
