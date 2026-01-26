namespace Bipins.AI.Trading.Application.Ports;

public class VectorDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public float[] Vector { get; init; } = Array.Empty<float>();
    public Dictionary<string, object> Payload { get; init; } = new();
}

public class VectorSearchResult
{
    public string Id { get; init; } = string.Empty;
    public float Score { get; init; }
    public Dictionary<string, object> Payload { get; init; } = new();
}

public interface IVectorMemoryStore
{
    Task UpsertAsync(string collection, VectorDocument document, CancellationToken cancellationToken = default);
    Task<List<VectorSearchResult>> SearchAsync(
        string collection,
        float[] vector,
        int topK = 10,
        Dictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default);
    Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default);
    Task CreateCollectionAsync(string collection, int vectorSize, CancellationToken cancellationToken = default);
}
