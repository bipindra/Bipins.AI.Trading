using Bipins.AI.Trading.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class ValueObjectsTests
{
    [Fact]
    public void Money_ShouldAddCorrectly()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");
        
        // Act
        var result = money1 + money2;
        
        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Money_ShouldSubtractCorrectly()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "USD");
        
        // Act
        var result = money1 - money2;
        
        // Assert
        result.Amount.Should().Be(70);
    }
    
    [Fact]
    public void Money_ShouldThrow_WhenAddingDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");
        
        // Act
        var act = () => money1 + money2;
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*USD*EUR*");
    }
    
    [Fact]
    public void Money_ShouldMultiplyCorrectly()
    {
        // Arrange
        var money = new Money(100, "USD");
        
        // Act
        var result = money * 2.5m;
        
        // Assert
        result.Amount.Should().Be(250);
    }
    
    [Fact]
    public void Money_ShouldIdentifyPositive()
    {
        // Arrange
        var money = new Money(100, "USD");
        
        // Assert
        money.IsPositive.Should().BeTrue();
        money.IsNegative.Should().BeFalse();
        money.IsZero.Should().BeFalse();
    }
    
    [Fact]
    public void Quantity_ShouldAddCorrectly()
    {
        // Arrange
        var qty1 = new Quantity(100);
        var qty2 = new Quantity(50);
        
        // Act
        var result = qty1 + qty2;
        
        // Assert
        result.Value.Should().Be(150);
    }
    
    [Fact]
    public void Quantity_ShouldSubtractCorrectly()
    {
        // Arrange
        var qty1 = new Quantity(100);
        var qty2 = new Quantity(30);
        
        // Act
        var result = qty1 - qty2;
        
        // Assert
        result.Value.Should().Be(70);
    }
    
    [Fact]
    public void Quantity_ShouldIdentifyPositive()
    {
        // Arrange
        var qty = new Quantity(100);
        
        // Assert
        qty.IsPositive.Should().BeTrue();
        qty.IsNegative.Should().BeFalse();
        qty.IsZero.Should().BeFalse();
    }
    
    [Fact]
    public void Quantity_Abs_ShouldReturnAbsoluteValue()
    {
        // Arrange
        var qty = new Quantity(-100);
        
        // Act
        var result = qty.Abs();
        
        // Assert
        result.Value.Should().Be(100);
    }
    
    [Fact]
    public void Timeframe_ShouldConvertToTimeSpan()
    {
        // Arrange
        var timeframe = new Timeframe("5m");
        
        // Act
        var timeSpan = timeframe.ToTimeSpan();
        
        // Assert
        timeSpan.Should().Be(TimeSpan.FromMinutes(5));
    }
    
    [Fact]
    public void Timeframe_ShouldThrow_WhenInvalid()
    {
        // Arrange
        var timeframe = new Timeframe("invalid");
        
        // Act
        var act = () => timeframe.ToTimeSpan();
        
        // Assert
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Symbol_ShouldImplicitlyConvertToString()
    {
        // Arrange
        var symbol = new Symbol("SPY");
        
        // Act
        string value = symbol;
        
        // Assert
        value.Should().Be("SPY");
    }
    
    [Fact]
    public void Symbol_ShouldImplicitlyConvertFromString()
    {
        // Act
        Symbol symbol = "SPY";
        
        // Assert
        symbol.Value.Should().Be("SPY");
    }
}
