namespace Bipins.AI.Trading.Application.Ports;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
