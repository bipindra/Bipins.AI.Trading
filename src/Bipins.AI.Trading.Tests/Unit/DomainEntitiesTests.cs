using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class DomainEntitiesTests
{
    [Fact]
    public void Candle_ShouldCalculateRange()
    {
        // Arrange
        var candle = new Candle
        {
            Symbol = new Symbol("SPY"),
            Timeframe = new Timeframe("5m"),
            Timestamp = DateTime.UtcNow,
            Open = 400m,
            High = 410m,
            Low = 390m,
            Close = 405m,
            Volume = 1000000
        };
        
        // Assert
        candle.Range.Should().Be(20m); // High - Low
        candle.Body.Should().Be(5m); // |Close - Open|
        candle.IsBullish.Should().BeTrue();
        candle.IsBearish.Should().BeFalse();
    }
    
    [Fact]
    public void Candle_GetIdempotencyKey_ShouldBeUnique()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var candle1 = new Candle
        {
            Symbol = new Symbol("SPY"),
            Timeframe = new Timeframe("5m"),
            Timestamp = timestamp,
            Open = 400m,
            High = 410m,
            Low = 390m,
            Close = 405m,
            Volume = 1000000
        };
        
        var candle2 = new Candle
        {
            Symbol = new Symbol("SPY"),
            Timeframe = new Timeframe("5m"),
            Timestamp = timestamp,
            Open = 400m,
            High = 410m,
            Low = 390m,
            Close = 405m,
            Volume = 1000000
        };
        
        // Act
        var key1 = candle1.GetIdempotencyKey();
        var key2 = candle2.GetIdempotencyKey();
        
        // Assert
        key1.Should().Be(key2);
        key1.Should().Contain("SPY");
        key1.Should().Contain("5m");
    }
    
    [Fact]
    public void Position_ShouldIdentifyLong()
    {
        // Arrange
        var position = new Position
        {
            Symbol = new Symbol("SPY"),
            Quantity = new Quantity(100),
            AveragePrice = new Money(400, "USD"),
            CurrentPrice = new Money(410, "USD"),
            OpenedAt = DateTime.UtcNow
        };
        
        // Assert
        position.IsLong.Should().BeTrue();
        position.IsShort.Should().BeFalse();
        position.IsFlat.Should().BeFalse();
    }
    
    [Fact]
    public void Position_ShouldCalculateMarketValue()
    {
        // Arrange
        var position = new Position
        {
            Symbol = new Symbol("SPY"),
            Quantity = new Quantity(100),
            AveragePrice = new Money(400, "USD"),
            CurrentPrice = new Money(410, "USD"),
            OpenedAt = DateTime.UtcNow
        };
        
        // Assert
        position.MarketValue.Amount.Should().Be(41000); // 100 * 410
        position.CostBasis.Amount.Should().Be(40000); // 100 * 400
    }
    
    [Fact]
    public void Portfolio_GetPosition_ShouldReturnPosition_WhenExists()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Positions = new List<Position>
            {
                new Position
                {
                    Symbol = new Symbol("SPY"),
                    Quantity = new Quantity(100)
                }
            }
        };
        
        // Act
        var position = portfolio.GetPosition(new Symbol("SPY"));
        
        // Assert
        position.Should().NotBeNull();
        position!.Symbol.Value.Should().Be("SPY");
    }
    
    [Fact]
    public void Portfolio_GetPosition_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Positions = new List<Position>
            {
                new Position
                {
                    Symbol = new Symbol("SPY"),
                    Quantity = new Quantity(100)
                }
            }
        };
        
        // Act
        var position = portfolio.GetPosition(new Symbol("QQQ"));
        
        // Assert
        position.Should().BeNull();
    }
    
    [Fact]
    public void Portfolio_TotalPnL_ShouldSumUnrealizedAndRealized()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            UnrealizedPnL = new Money(1000, "USD"),
            RealizedPnL = new Money(500, "USD")
        };
        
        // Assert
        portfolio.TotalPnL.Amount.Should().Be(1500);
    }
    
    [Fact]
    public void TradeDecision_GetIdempotencyKey_ShouldBeUnique()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var decision1 = new TradeDecision
        {
            Symbol = new Symbol("SPY"),
            Timeframe = new Timeframe("5m"),
            CandleTimestamp = timestamp,
            Action = TradeAction.Buy,
            Confidence = 0.8m,
            Rationale = "Test"
        };
        
        var decision2 = new TradeDecision
        {
            Symbol = new Symbol("SPY"),
            Timeframe = new Timeframe("5m"),
            CandleTimestamp = timestamp,
            Action = TradeAction.Buy,
            Confidence = 0.8m,
            Rationale = "Test"
        };
        
        // Act
        var key1 = decision1.GetIdempotencyKey();
        var key2 = decision2.GetIdempotencyKey();
        
        // Assert
        key1.Should().Be(key2);
        key1.Should().Contain("SPY");
        key1.Should().Contain("5m");
    }
}
