namespace Bipins.AI.Trading.Domain.ValueObjects;

public record Quantity(decimal Value)
{
    public static Quantity Zero => new(0m);
    
    public static Quantity operator +(Quantity left, Quantity right) =>
        new Quantity(left.Value + right.Value);
    
    public static Quantity operator -(Quantity left, Quantity right) =>
        new Quantity(left.Value - right.Value);
    
    public static Quantity operator *(Quantity quantity, decimal multiplier) =>
        new Quantity(quantity.Value * multiplier);
    
    public bool IsPositive => Value > 0;
    public bool IsNegative => Value < 0;
    public bool IsZero => Value == 0;
    
    public Quantity Abs() => new Quantity(Math.Abs(Value));
}
