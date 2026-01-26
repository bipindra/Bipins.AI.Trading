using Bipins.AI.Trading.Application.Ports;

namespace Bipins.AI.Trading.Application.Infrastructure;

public class Clock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}
