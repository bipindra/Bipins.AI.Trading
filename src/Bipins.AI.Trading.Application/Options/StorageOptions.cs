namespace Bipins.AI.Trading.Application.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";
    
    public string DbProvider { get; set; } = "InMemorySqlite";
    public string ConnectionString { get; set; } = "Data Source=:memory:";
}
