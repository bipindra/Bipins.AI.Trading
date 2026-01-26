namespace Bipins.AI.Trading.Domain.ValueObjects;

public record Money(decimal Amount, string Currency = "USD")
{
    public static Money Zero => new(0m);
    
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add {left.Currency} and {right.Currency}");
        return new Money(left.Amount + right.Amount, left.Currency);
    }
    
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract {left.Currency} and {right.Currency}");
        return new Money(left.Amount - right.Amount, left.Currency);
    }
    
    public static Money operator *(Money money, decimal multiplier) =>
        new Money(money.Amount * multiplier, money.Currency);
    
    public static Money operator /(Money money, decimal divisor) =>
        new Money(money.Amount / divisor, money.Currency);
    
    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;
}
