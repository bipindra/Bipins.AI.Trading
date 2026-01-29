namespace Bipins.AI.Trading.Application.Options;

public class VectorDbOptions
{
    public const string SectionName = "VectorDb";
    
    public string Provider { get; set; } = "Qdrant";
    
    public QdrantOptions Qdrant { get; set; } = new();
    public PineconeOptions Pinecone { get; set; } = new();
    public MilvusOptions Milvus { get; set; } = new();
    public WeaviateOptions Weaviate { get; set; } = new();
}

public class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "trading_decisions";
}

public class PineconeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string IndexName { get; set; } = "trading-decisions";
}

public class MilvusOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 19530;
    public string CollectionName { get; set; } = "trading_decisions";
}

public class WeaviateOptions
{
    public string Endpoint { get; set; } = "http://localhost:8080";
    public string ApiKey { get; set; } = string.Empty;
    public string ClassName { get; set; } = "TradingDecision";
}
