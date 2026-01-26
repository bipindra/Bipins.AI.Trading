namespace Bipins.AI.Trading.Infrastructure.Resilience;

public interface IResilienceService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, string serviceName, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> action, string serviceName, CancellationToken cancellationToken = default);
    CircuitBreakerState GetCircuitBreakerState(string serviceName);
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen,
    Isolated
}
