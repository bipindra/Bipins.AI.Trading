namespace Bipins.AI.Trading.Application.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";
    
    public string Provider { get; set; } = "Memory";
    public string? RedisConnectionString { get; set; }
}
