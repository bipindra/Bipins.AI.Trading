using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.TickData;

public interface ITickDataProvider
{
    Task<List<Tick>> GetTicksAsync(
        Symbol symbol,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
    
    Task<Tick?> GetLatestTickAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default);
    
    Task SubscribeTicksAsync(
        Symbol symbol,
        Func<Tick, Task> handler,
        CancellationToken cancellationToken = default);
    
    Task UnsubscribeTicksAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
