using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Services;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class RiskManagerTests
{
    private readonly RiskManager _riskManager;
    private readonly RiskOptions _options;
    
    public RiskManagerTests()
    {
        _options = new RiskOptions
        {
            MaxPositionPercent = 10.0,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5.0,
            AtrLimit = 2.0,
            SpreadLimit = 0.01
        };
        
        var optionsMock = new Mock<IOptions<RiskOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);
        
        var loggerMock = new Mock<ILogger<RiskManager>>();
        
        _riskManager = new RiskManager(optionsMock.Object, loggerMock.Object);
    }
    
    [Fact]
    public async Task CheckTradeAsync_ShouldReject_WhenPositionExceedsMaxPercent()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            Positions = new List<Position>
            {
                new Position
                {
                    Symbol = new Symbol("SPY"),
                    Quantity = new Quantity(100),
                    AveragePrice = new Money(400, "USD"),
                    CurrentPrice = new Money(400, "USD")
                }
            }
        };
        
        var decision = new TradeDecision
        {
            Symbol = new Symbol("SPY"),
            Action = TradeAction.Buy,
            QuantityPercent = 15.0m // Exceeds 10%
        };
        
        // Act
        var result = await _riskManager.CheckTradeAsync(decision, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("exceed max position percent");
    }
    
    [Fact]
    public async Task CheckTradeAsync_ShouldAllow_WhenPositionWithinLimit()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            Positions = new List<Position>()
        };
        
        var decision = new TradeDecision
        {
            Symbol = new Symbol("SPY"),
            Action = TradeAction.Buy,
            QuantityPercent = 5.0m // Within limit
        };
        
        // Act
        var result = await _riskManager.CheckTradeAsync(decision, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeTrue();
    }
    
    [Fact]
    public async Task CheckTradeAsync_ShouldReject_WhenMaxPositionsReached()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            Positions = new List<Position>
            {
                new Position { Symbol = new Symbol("SPY"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("QQQ"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("AAPL"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("MSFT"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("GOOGL"), Quantity = new Quantity(10) }
            }
        };
        
        var decision = new TradeDecision
        {
            Symbol = new Symbol("TSLA"),
            Action = TradeAction.Buy,
            QuantityPercent = 2.0m
        };
        
        // Act
        var result = await _riskManager.CheckTradeAsync(decision, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("Max open positions");
    }
    
    [Fact]
    public async Task CheckTradeAsync_ShouldAllow_WhenReplacingExistingPosition()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            Positions = new List<Position>
            {
                new Position { Symbol = new Symbol("SPY"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("QQQ"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("AAPL"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("MSFT"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("GOOGL"), Quantity = new Quantity(10) }
            }
        };
        
        var decision = new TradeDecision
        {
            Symbol = new Symbol("SPY"), // Existing position
            Action = TradeAction.Buy,
            QuantityPercent = 2.0m
        };
        
        // Act
        var result = await _riskManager.CheckTradeAsync(decision, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeTrue(); // Can replace existing position
    }
    
    [Fact]
    public async Task CheckTradeAsync_ShouldReject_WhenDailyLossLimitExceeded()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(100000, "USD"),
            UnrealizedPnL = new Money(-6000, "USD"), // 6% loss, exceeds 5%
            RealizedPnL = Money.Zero
        };
        
        var decision = new TradeDecision
        {
            Symbol = new Symbol("SPY"),
            Action = TradeAction.Buy,
            QuantityPercent = 2.0m
        };
        
        // Act
        var result = await _riskManager.CheckTradeAsync(decision, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("Daily loss limit");
    }
    
    [Fact]
    public async Task CheckOrderAsync_ShouldReject_WhenInsufficientBuyingPower()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(50000, "USD"),
            BuyingPower = new Money(10000, "USD")
        };
        
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = new Quantity(100),
            LimitPrice = new Money(500, "USD") // 50,000 required, only 10,000 available
        };
        
        // Act
        var result = await _riskManager.CheckOrderAsync(order, portfolio);
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient buying power");
    }
    
    [Fact]
    public async Task CheckDailyLossLimitAsync_ShouldReturnFalse_WhenLossExceedsLimit()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(100000, "USD"),
            UnrealizedPnL = new Money(-6000, "USD"), // 6% loss
            RealizedPnL = Money.Zero
        };
        
        // Act
        var result = await _riskManager.CheckDailyLossLimitAsync(portfolio);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task CheckDailyLossLimitAsync_ShouldReturnTrue_WhenLossWithinLimit()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Equity = new Money(100000, "USD"),
            Cash = new Money(100000, "USD"),
            UnrealizedPnL = new Money(-3000, "USD"), // 3% loss
            RealizedPnL = Money.Zero
        };
        
        // Act
        var result = await _riskManager.CheckDailyLossLimitAsync(portfolio);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task CheckMaxPositionsAsync_ShouldReturnFalse_WhenMaxReached()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Positions = new List<Position>
            {
                new Position { Symbol = new Symbol("SPY"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("QQQ"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("AAPL"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("MSFT"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("GOOGL"), Quantity = new Quantity(10) }
            }
        };
        
        // Act
        var result = await _riskManager.CheckMaxPositionsAsync(portfolio);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task CheckMaxPositionsAsync_ShouldReturnTrue_WhenUnderLimit()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Positions = new List<Position>
            {
                new Position { Symbol = new Symbol("SPY"), Quantity = new Quantity(10) },
                new Position { Symbol = new Symbol("QQQ"), Quantity = new Quantity(10) }
            }
        };
        
        // Act
        var result = await _riskManager.CheckMaxPositionsAsync(portfolio);
        
        // Assert
        result.Should().BeTrue();
    }
}
