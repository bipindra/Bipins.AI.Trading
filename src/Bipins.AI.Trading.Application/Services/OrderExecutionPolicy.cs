using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Application.Services;

public class OrderExecutionPolicy : IOrderExecutionPolicy
{
    private readonly ILogger<OrderExecutionPolicy> _logger;
    
    public OrderExecutionPolicy(ILogger<OrderExecutionPolicy> logger)
    {
        _logger = logger;
    }
    
    public Task<bool> CanExecuteAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Basic validation
        if (order.Quantity.IsZero)
        {
            _logger.LogWarning("Order {OrderId} has zero quantity", order.Id);
            return Task.FromResult(false);
        }
        
        if (order.Type == OrderType.Limit && order.LimitPrice == null)
        {
            _logger.LogWarning("Order {OrderId} is limit order but has no limit price", order.Id);
            return Task.FromResult(false);
        }
        
        if (order.Type == OrderType.Stop && order.StopPrice == null)
        {
            _logger.LogWarning("Order {OrderId} is stop order but has no stop price", order.Id);
            return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }
    
    public Task ApplyGuardrailsAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Apply any additional guardrails here
        // For example: round quantities, validate prices, etc.
        
        _logger.LogDebug("Applied guardrails to order {OrderId}", order.Id);
        return Task.CompletedTask;
    }
}
