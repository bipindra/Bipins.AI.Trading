using Bipins.AI.Trading.Application.Services;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class OrderExecutionPolicyTests
{
    private readonly OrderExecutionPolicy _policy;
    
    public OrderExecutionPolicyTests()
    {
        var loggerMock = new Mock<ILogger<OrderExecutionPolicy>>();
        _policy = new OrderExecutionPolicy(loggerMock.Object);
    }
    
    [Fact]
    public async Task CanExecuteAsync_ShouldReturnFalse_WhenQuantityIsZero()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = Quantity.Zero
        };
        
        // Act
        var result = await _policy.CanExecuteAsync(order);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task CanExecuteAsync_ShouldReturnFalse_WhenLimitOrderHasNoLimitPrice()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = new Quantity(10),
            LimitPrice = null
        };
        
        // Act
        var result = await _policy.CanExecuteAsync(order);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task CanExecuteAsync_ShouldReturnFalse_WhenStopOrderHasNoStopPrice()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Stop,
            Quantity = new Quantity(10),
            StopPrice = null
        };
        
        // Act
        var result = await _policy.CanExecuteAsync(order);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task CanExecuteAsync_ShouldReturnTrue_WhenMarketOrderIsValid()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = new Quantity(10)
        };
        
        // Act
        var result = await _policy.CanExecuteAsync(order);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task CanExecuteAsync_ShouldReturnTrue_WhenLimitOrderHasLimitPrice()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = new Quantity(10),
            LimitPrice = new Money(400, "USD")
        };
        
        // Act
        var result = await _policy.CanExecuteAsync(order);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task ApplyGuardrailsAsync_ShouldComplete_WithoutException()
    {
        // Arrange
        var order = new Order
        {
            Symbol = new Symbol("SPY"),
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = new Quantity(10)
        };
        
        // Act
        var act = async () => await _policy.ApplyGuardrailsAsync(order);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
}
