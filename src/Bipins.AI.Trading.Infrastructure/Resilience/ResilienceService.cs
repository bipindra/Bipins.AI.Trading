using Bipins.AI.Trading.Application.Options;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Bipins.AI.Trading.Infrastructure.Resilience;

public class ResilienceService : IResilienceService
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResilienceService> _logger;
    private readonly Dictionary<string, ResiliencePipeline> _policies = new();
    private readonly Dictionary<string, CircuitBreakerState> _circuitStates = new();
    private readonly object _lockObject = new();

    public ResilienceService(IOptions<ResilienceOptions> options, ILogger<ResilienceService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private ResiliencePipeline GetOrCreatePolicy(string serviceName)
    {
        if (_policies.TryGetValue(serviceName, out var existingPolicy))
        {
            return existingPolicy;
        }

        lock (_lockObject)
        {
            if (_policies.TryGetValue(serviceName, out existingPolicy))
            {
                return existingPolicy;
            }

            var serviceOptions = GetServiceOptions(serviceName);
            var policy = CreateResiliencePipeline(serviceName, serviceOptions);
            _policies[serviceName] = policy;
            _circuitStates[serviceName] = CircuitBreakerState.Closed;

            return policy;
        }
    }

    private ServiceResilienceOptions GetServiceOptions(string serviceName)
    {
        if (_options.Services.TryGetValue(serviceName, out var serviceOptions))
        {
            return serviceOptions;
        }

        // Return defaults from global options
        return new ServiceResilienceOptions
        {
            FailureThreshold = _options.CircuitBreaker.FailureThreshold,
            DurationOfBreak = _options.CircuitBreaker.DurationOfBreak,
            RetryMaxAttempts = _options.Retry.MaxAttempts,
            RetryBaseDelay = _options.Retry.BaseDelay,
            RetryMaxDelay = _options.Retry.MaxDelay
        };
    }

    private ResiliencePipeline CreateResiliencePipeline(string serviceName, ServiceResilienceOptions serviceOptions)
    {
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = serviceOptions.RetryMaxAttempts,
            Delay = serviceOptions.RetryBaseDelay ?? _options.Retry.BaseDelay,
            MaxDelay = serviceOptions.RetryMaxDelay ?? _options.Retry.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                .Handle<TaskCanceledException>()
                .Handle<TimeoutException>()
                .Handle<BrokenCircuitException>(),
            OnRetry = args =>
            {
                _logger.LogWarning(
                    args.Outcome.Exception,
                    "Retry {RetryCount}/{MaxAttempts} for {ServiceName} after {Delay}ms",
                    args.AttemptNumber, serviceOptions.RetryMaxAttempts, serviceName, args.RetryDelay.TotalMilliseconds);
                return ValueTask.CompletedTask;
            }
        };

        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5, // 50% failure rate
            SamplingDuration = _options.CircuitBreaker.SamplingDuration,
            MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
            BreakDuration = serviceOptions.DurationOfBreak,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                .Handle<TaskCanceledException>()
                .Handle<TimeoutException>(),
            OnOpened = args =>
            {
                _circuitStates[serviceName] = CircuitBreakerState.Open;
                _logger.LogError(
                    args.Outcome.Exception,
                    "Circuit breaker opened for {ServiceName}. Will remain open for {Duration}",
                    serviceName, serviceOptions.DurationOfBreak);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                _circuitStates[serviceName] = CircuitBreakerState.Closed;
                _logger.LogInformation("Circuit breaker reset for {ServiceName}", serviceName);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                _circuitStates[serviceName] = CircuitBreakerState.HalfOpen;
                _logger.LogInformation("Circuit breaker half-open for {ServiceName}. Testing connection...", serviceName);
                return ValueTask.CompletedTask;
            }
        };

        var pipelineBuilder = new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .AddCircuitBreaker(circuitBreakerOptions);

        return pipelineBuilder.Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string serviceName, CancellationToken cancellationToken = default)
    {
        var policy = GetOrCreatePolicy(serviceName);
        
        try
        {
            return await policy.ExecuteAsync(async (ct) => await action(), cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open for {ServiceName}. Request rejected.", serviceName);
            throw new InvalidOperationException($"Service {serviceName} is currently unavailable due to circuit breaker being open.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed for {ServiceName} after all retries", serviceName);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action, string serviceName, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, serviceName, cancellationToken);
    }

    public CircuitBreakerState GetCircuitBreakerState(string serviceName)
    {
        return _circuitStates.TryGetValue(serviceName, out var state) ? state : CircuitBreakerState.Closed;
    }
}
