namespace Bipins.AI.Trading.Application.Options;

public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
    public Dictionary<string, ServiceResilienceOptions> Services { get; set; } = new();
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int MinimumThroughput { get; set; } = 2;
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public double BackoffCoefficient { get; set; } = 2.0;
}

public class ServiceResilienceOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
    public int RetryMaxAttempts { get; set; } = 3;
    public TimeSpan? RetryBaseDelay { get; set; }
    public TimeSpan? RetryMaxDelay { get; set; }
}
