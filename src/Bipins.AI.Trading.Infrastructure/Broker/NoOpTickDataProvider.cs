using Bipins.AI.Trading.Application.TickData;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Infrastructure.Broker;

internal class NoOpTickDataProvider : ITickDataProvider
{
    public Task<List<Tick>> GetTicksAsync(Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Tick>());
    
    public Task<Tick?> GetLatestTickAsync(Symbol symbol, CancellationToken cancellationToken = default)
        => Task.FromResult<Tick?>(null);
    
    public Task SubscribeTicksAsync(Symbol symbol, Func<Tick, Task> handler, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    
    public Task UnsubscribeTicksAsync(Symbol symbol, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
