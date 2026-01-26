# Resilience Patterns

This document describes the circuit breaker and retry policies implemented in the Bipins.AI.Trading platform.

## Overview

The platform implements resilience patterns using the Polly library to handle transient failures and prevent cascading failures when calling external APIs:

- **Circuit Breaker**: Prevents repeated calls to failing services
- **Retry Policy**: Automatically retries failed requests with exponential backoff
- **Service-Specific Configuration**: Each external service (Alpaca, OpenAI, Anthropic, etc.) can have its own resilience settings

## Configuration

Resilience settings are configured in `appsettings.json` under the `Resilience` section:

```json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "DurationOfBreak": "00:00:30",
      "SamplingDuration": "00:01:00",
      "MinimumThroughput": 2
    },
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelay": "00:00:01",
      "MaxDelay": "00:00:30",
      "BackoffCoefficient": 2.0
    },
    "Services": {
      "Alpaca": {
        "FailureThreshold": 5,
        "DurationOfBreak": "00:00:30",
        "RetryMaxAttempts": 3
      },
      "OpenAI": {
        "FailureThreshold": 3,
        "DurationOfBreak": "00:01:00",
        "RetryMaxAttempts": 2
      }
    }
  }
}
```

### Global Settings

- **CircuitBreaker.FailureThreshold**: Number of consecutive failures before opening the circuit
- **CircuitBreaker.DurationOfBreak**: How long the circuit stays open before attempting to close
- **CircuitBreaker.SamplingDuration**: Time window for measuring failure rate
- **CircuitBreaker.MinimumThroughput**: Minimum number of calls required before circuit can open
- **Retry.MaxAttempts**: Maximum number of retry attempts
- **Retry.BaseDelay**: Initial delay before first retry
- **Retry.MaxDelay**: Maximum delay between retries
- **Retry.BackoffCoefficient**: Multiplier for exponential backoff (e.g., 2.0 = doubles each retry)

### Service-Specific Settings

Each service can override global settings:
- **FailureThreshold**: Override circuit breaker failure threshold
- **DurationOfBreak**: Override circuit breaker duration
- **RetryMaxAttempts**: Override retry attempts
- **RetryBaseDelay**: Override base retry delay (optional)
- **RetryMaxDelay**: Override max retry delay (optional)

## Circuit Breaker States

1. **Closed**: Normal operation, requests pass through
2. **Open**: Circuit is open, requests are immediately rejected
3. **HalfOpen**: Testing if service has recovered, allows limited requests
4. **Isolated**: Manually isolated (not used in current implementation)

## Retry Policy

The retry policy uses exponential backoff with jitter:

- Base delay: 1 second (configurable)
- Backoff coefficient: 2.0 (doubles each retry)
- Jitter: ±20% random variation to prevent thundering herd
- Max delay: 30 seconds (configurable)

Example retry delays:
- Attempt 1: ~1 second
- Attempt 2: ~2 seconds
- Attempt 3: ~4 seconds

## Supported Services

The following services have resilience policies applied:

- **Alpaca**: Broker and market data API calls
- **OpenAI**: Chat and embedding API calls
- **Anthropic**: Chat API calls
- **AzureOpenAI**: Chat and embedding API calls
- **Qdrant**: Vector database operations

## Error Handling

### Retryable Exceptions

The following exceptions trigger retries:
- `HttpRequestException`: Network or HTTP errors
- `TaskCanceledException`: Request timeouts
- `TimeoutException`: Operation timeouts
- `BrokenCircuitException`: Circuit breaker is open (retried when circuit closes)

### Non-Retryable Exceptions

The following exceptions are NOT retried:
- `InvalidOperationException`: Configuration errors
- `ArgumentException`: Invalid parameters
- `UnauthorizedAccessException`: Authentication failures (401)
- `HttpRequestException` with 4xx status codes (except 429)

## Monitoring

Circuit breaker state changes are logged:
- **Circuit Opened**: Error level log when circuit opens
- **Circuit Reset**: Information level log when circuit closes
- **Half-Open**: Information level log when testing recovery
- **Retry Attempts**: Warning level log for each retry

## Best Practices

1. **Configure service-specific settings** for critical services (e.g., Alpaca trading)
2. **Use shorter durations** for services that recover quickly
3. **Use longer durations** for services that may have extended outages
4. **Monitor circuit breaker states** in production
5. **Adjust retry attempts** based on service SLA requirements

## Example: Checking Circuit Breaker State

```csharp
var resilienceService = serviceProvider.GetRequiredService<IResilienceService>();
var state = resilienceService.GetCircuitBreakerState("Alpaca");

switch (state)
{
    case CircuitBreakerState.Closed:
        // Service is healthy
        break;
    case CircuitBreakerState.Open:
        // Service is failing, circuit is open
        break;
    case CircuitBreakerState.HalfOpen:
        // Testing if service recovered
        break;
}
```

## Troubleshooting

### Circuit Breaker Stuck Open

If a circuit breaker remains open longer than expected:
1. Check service health (Alpaca, LLM provider, etc.)
2. Verify network connectivity
3. Review error logs for root cause
4. Consider manually resetting (requires application restart with updated config)

### Too Many Retries

If you see excessive retry attempts:
1. Reduce `RetryMaxAttempts` for the service
2. Check if exceptions are truly transient
3. Review service SLA and adjust expectations

### Circuit Opens Too Quickly

If circuit opens after few failures:
1. Increase `FailureThreshold` for the service
2. Verify failures are not due to configuration issues
3. Check `MinimumThroughput` setting
