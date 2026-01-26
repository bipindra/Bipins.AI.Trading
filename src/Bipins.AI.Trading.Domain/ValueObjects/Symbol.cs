namespace Bipins.AI.Trading.Domain.ValueObjects;

public record Symbol(string Value)
{
    public static implicit operator string(Symbol symbol) => symbol.Value;
    public static implicit operator Symbol(string value) => new(value);
    
    public override string ToString() => Value;
}
