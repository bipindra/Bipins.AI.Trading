using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Ports;

public interface IOrderExecutionPolicy
{
    Task<bool> CanExecuteAsync(Order order, CancellationToken cancellationToken = default);
    Task ApplyGuardrailsAsync(Order order, CancellationToken cancellationToken = default);
}
