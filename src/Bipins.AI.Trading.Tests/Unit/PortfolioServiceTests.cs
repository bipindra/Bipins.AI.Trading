using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Services;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class PortfolioServiceTests
{
    private readonly Mock<IBrokerClient> _brokerClientMock;
    private readonly PortfolioService _portfolioService;
    
    public PortfolioServiceTests()
    {
        _brokerClientMock = new Mock<IBrokerClient>();
        var loggerMock = new Mock<ILogger<PortfolioService>>();
        
        _portfolioService = new PortfolioService(_brokerClientMock.Object, loggerMock.Object);
    }
    
    [Fact]
    public async Task GetCurrentPortfolioAsync_ShouldReturnPortfolio_WithAccountInfo()
    {
        // Arrange
        var accountInfo = new AccountInfo
        {
            Cash = new Money(50000, "USD"),
            Equity = new Money(100000, "USD"),
            BuyingPower = new Money(100000, "USD"),
            AccountNumber = "TEST123"
        };
        
        var positions = new List<Position>
        {
            new Position
            {
                Symbol = new Symbol("SPY"),
                Quantity = new Quantity(100),
                AveragePrice = new Money(400, "USD"),
                CurrentPrice = new Money(410, "USD")
            }
        };
        
        _brokerClientMock.Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountInfo);
        _brokerClientMock.Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);
        
        // Act
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
        
        // Assert
        portfolio.Should().NotBeNull();
        portfolio.Cash.Should().Be(accountInfo.Cash);
        portfolio.Equity.Should().Be(accountInfo.Equity);
        portfolio.BuyingPower.Should().Be(accountInfo.BuyingPower);
        portfolio.Positions.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task GetCurrentPortfolioAsync_ShouldCalculateUnrealizedPnL()
    {
        // Arrange
        var accountInfo = new AccountInfo
        {
            Cash = new Money(50000, "USD"),
            Equity = new Money(100000, "USD"),
            BuyingPower = new Money(100000, "USD")
        };
        
        var positions = new List<Position>
        {
            new Position
            {
                Symbol = new Symbol("SPY"),
                Quantity = new Quantity(100),
                AveragePrice = new Money(400, "USD"), // Cost: 40,000
                CurrentPrice = new Money(410, "USD")  // Value: 41,000
            }
        };
        
        _brokerClientMock.Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountInfo);
        _brokerClientMock.Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);
        
        // Act
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
        
        // Assert
        portfolio.UnrealizedPnL.Amount.Should().Be(1000); // 41,000 - 40,000
    }
    
    [Fact]
    public async Task GetPositionPercentAsync_ShouldReturnZero_WhenNoPosition()
    {
        // Arrange
        var accountInfo = new AccountInfo
        {
            Equity = new Money(100000, "USD")
        };
        
        _brokerClientMock.Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountInfo);
        _brokerClientMock.Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position>());
        
        // Act
        var percent = await _portfolioService.GetPositionPercentAsync(new Symbol("SPY"));
        
        // Assert
        percent.Should().Be(0m);
    }
    
    [Fact]
    public async Task GetPositionPercentAsync_ShouldCalculateCorrectPercent()
    {
        // Arrange
        var accountInfo = new AccountInfo
        {
            Equity = new Money(100000, "USD")
        };
        
        var positions = new List<Position>
        {
            new Position
            {
                Symbol = new Symbol("SPY"),
                Quantity = new Quantity(100),
                AveragePrice = new Money(400, "USD"),
                CurrentPrice = new Money(400, "USD") // Market value: 40,000
            }
        };
        
        _brokerClientMock.Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountInfo);
        _brokerClientMock.Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);
        
        // Act
        var percent = await _portfolioService.GetPositionPercentAsync(new Symbol("SPY"));
        
        // Assert
        percent.Should().Be(40.0m); // 40,000 / 100,000 * 100
    }
    
    [Fact]
    public async Task UpdatePortfolioAsync_ShouldUpdateLastUpdatedAt()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Cash = new Money(50000, "USD"),
            Equity = new Money(100000, "USD")
        };
        
        var originalTime = portfolio.LastUpdatedAt;
        
        // Act
        await Task.Delay(10); // Small delay
        await _portfolioService.UpdatePortfolioAsync(portfolio);
        
        // Assert
        portfolio.LastUpdatedAt.Should().BeAfter(originalTime);
    }
}
