# Circuit Breaker and Retry Policies Implementation

## Overview
Implement circuit breaker pattern and retry policies with exponential backoff for external API calls (Alpaca broker, LLM providers). This will improve system resilience and handle transient failures gracefully.

## Goals
- Add circuit breaker pattern to prevent cascading failures
- Implement retry policies with exponential backoff for transient errors
- Apply to all external API calls: Alpaca, OpenAI, Anthropic, Azure OpenAI, Qdrant
- Make policies configurable via appsettings.json
- Add comprehensive logging for circuit breaker state changes

## Files to Create/Update

### 1. Add Resilience NuGet Package
- **File**: `src/Bipins.AI.Trading.Infrastructure/Bipins.AI.Trading.Infrastructure.csproj`
- **Action**: Add `Polly` package for circuit breaker and retry policies
- **Package**: `Polly` (latest stable version)

### 2. Create Resilience Options
- **File**: `src/Bipins.AI.Trading.Application/Options/ResilienceOptions.cs`
- **Purpose**: Configuration for circuit breaker and retry policies
- **Properties**:
  - CircuitBreakerFailureThreshold (default: 5)
  - CircuitBreakerDurationOfBreak (default: 30 seconds)
  - RetryMaxAttempts (default: 3)
  - RetryBaseDelay (default: 1 second)
  - RetryMaxDelay (default: 30 seconds)

### 3. Create Circuit Breaker Service
- **File**: `src/Bipins.AI.Trading.Infrastructure/Resilience/CircuitBreakerService.cs`
- **Purpose**: Wrapper around Polly circuit breaker
- **Interface**: `ICircuitBreakerService`
- **Methods**:
  - `ExecuteAsync<T>(Func<Task<T>> action, string policyName)`
  - `GetState(string policyName)` - Returns circuit breaker state

### 4. Create Retry Policy Service
- **File**: `src/Bipins.AI.Trading.Infrastructure/Resilience/RetryPolicyService.cs`
- **Purpose**: Wrapper around Polly retry policy
- **Interface**: `IRetryPolicyService`
- **Methods**:
  - `ExecuteAsync<T>(Func<Task<T>> action, string policyName)`

### 5. Create Combined Resilience Service
- **File**: `src/Bipins.AI.Trading.Infrastructure/Resilience/ResilienceService.cs`
- **Purpose**: Combines circuit breaker and retry policies
- **Interface**: `IResilienceService`
- **Methods**:
  - `ExecuteAsync<T>(Func<Task<T>> action, string serviceName)`
  - Uses retry policy wrapped in circuit breaker

### 6. Update Alpaca Broker Client
- **File**: `src/Bipins.AI.Trading.Infrastructure/Broker/Alpaca/AlpacaBrokerClient.cs`
- **Changes**:
  - Inject `IResilienceService`
  - Wrap all API calls (GetAccountAsync, GetPositionsAsync, SubmitOrderAsync, etc.) with resilience service
  - Add logging for circuit breaker state changes

### 7. Update Alpaca Market Data Client
- **File**: `src/Bipins.AI.Trading.Infrastructure/Broker/Alpaca/AlpacaMarketDataClient.cs`
- **Changes**:
  - Inject `IResilienceService`
  - Wrap GetHistoricalCandlesAsync and other API calls

### 8. Update LLM Providers
- **Files**:
  - `src/Bipins.AI.Trading.Infrastructure/BipinsAI/LLM/Providers/OpenAIProvider.cs`
  - `src/Bipins.AI.Trading.Infrastructure/BipinsAI/LLM/Providers/AnthropicProvider.cs`
  - `src/Bipins.AI.Trading.Infrastructure/BipinsAI/LLM/Providers/AzureOpenAIProvider.cs`
- **Changes**:
  - Inject `IResilienceService`
  - Wrap ChatAsync, ChatWithFunctionsAsync, GenerateEmbeddingAsync calls

### 9. Update Qdrant Vector Store (if applicable)
- **File**: `src/Bipins.AI.Trading.Infrastructure/Vector/BipinsAIVectorAdapter.cs`
- **Changes**: Wrap vector store operations if Qdrant client makes HTTP calls

### 10. Update Dependency Injection
- **File**: `src/Bipins.AI.Trading.Infrastructure/DependencyInjection.cs`
- **Changes**:
  - Register `IResilienceService`, `ICircuitBreakerService`, `IRetryPolicyService`
  - Configure Polly policies based on `ResilienceOptions`
  - Create named policies for each service (Alpaca, OpenAI, Anthropic, AzureOpenAI, Qdrant)

### 11. Update appsettings.json
- **File**: `src/Bipins.AI.Trading.Web/appsettings.json`
- **Changes**: Add `Resilience` section with default values

### 12. Create Resilience Documentation
- **File**: `docs/08-resilience.md`
- **Purpose**: Document circuit breaker and retry policies, configuration, monitoring

## Implementation Details

### Circuit Breaker Pattern
- **States**: Closed (normal), Open (failing), HalfOpen (testing)
- **Failure Threshold**: Number of consecutive failures before opening circuit
- **Duration of Break**: Time to wait before attempting half-open
- **Monitoring**: Log state changes and failure counts

### Retry Policy
- **Strategy**: Exponential backoff with jitter
- **Max Attempts**: Configurable (default: 3)
- **Base Delay**: Initial delay before first retry
- **Max Delay**: Maximum delay between retries
- **Retryable Exceptions**: HttpRequestException, TimeoutException, TaskCanceledException

### Service-Specific Policies
- **Alpaca**: Separate policies for trading and market data
- **LLM Providers**: Separate policies for each provider (OpenAI, Anthropic, AzureOpenAI)
- **Qdrant**: Policy for vector store operations

### Error Handling
- **Circuit Breaker Open**: Return meaningful error, don't attempt call
- **Retry Exhausted**: Log error and throw original exception
- **Non-Transient Errors**: Don't retry (e.g., 401 Unauthorized, 400 Bad Request)

## Configuration Example

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
      },
      "Qdrant": {
        "FailureThreshold": 5,
        "DurationOfBreak": "00:00:15",
        "RetryMaxAttempts": 3
      }
    }
  }
}
```

## Testing Strategy
- Unit tests for circuit breaker state transitions
- Unit tests for retry policy behavior
- Integration tests with mock HTTP clients
- Test circuit breaker opening and recovery
- Test retry exhaustion scenarios

## Dependencies
- Polly (NuGet package)
- Microsoft.Extensions.Http.Polly (if using IHttpClientBuilder)

## Success Criteria
- All external API calls wrapped with resilience policies
- Circuit breaker prevents cascading failures
- Retry policies handle transient errors
- Configuration is flexible and service-specific
- Comprehensive logging for monitoring
- No breaking changes to existing interfaces
