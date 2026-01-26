namespace Bipins.AI.Trading.Application.Options;

public class VectorDbOptions
{
    public const string SectionName = "VectorDb";
    
    public string Provider { get; set; } = "Qdrant";
    
    public QdrantOptions Qdrant { get; set; } = new();
}

public class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "trading_decisions";
}
