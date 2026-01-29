// Adapter to bridge Bipins.AI.Vector.IVectorStore (from NuGet package) to Application.Ports.IVectorMemoryStore
// Works with any IVectorStore implementation from Bipins.AI - no provider-specific dependencies

using AppVector = Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Vector;

namespace Bipins.AI.Trading.Infrastructure.Vector;

public class VectorStoreAdapter : AppVector.IVectorMemoryStore
{
    private readonly IVectorStore _store;

    public VectorStoreAdapter(IVectorStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }
    
    public static IVectorMemoryStore CreateAdapter(IVectorStore store)
    {
        return new VectorStoreAdapter(store);
    }
    
    public async Task UpsertAsync(string collection, AppVector.VectorDocument document, CancellationToken cancellationToken = default)
    {
        var record = new VectorRecord(
            document.Id,
            document.Vector,
            string.Empty,
            document.Payload,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        var request = new VectorUpsertRequest(
            new[] { record },
            collection);

        await _store.UpsertAsync(request, cancellationToken);
    }
    
    public async Task<List<AppVector.VectorSearchResult>> SearchAsync(
        string collection,
        float[] vector,
        int topK = 10,
        Dictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default)
    {
        // NOTE: Bipins.AI currently expects provider-specific filters; we ignore the
        //       incoming filter dictionary for now and rely on pure vector similarity.

        var query = new VectorQueryRequest(
            vector,
            topK,
            string.Empty,
            null,
            collection);

        var response = await _store.QueryAsync(query, cancellationToken);

        var results = new List<AppVector.VectorSearchResult>();
        foreach (var match in response.Matches)
        {
            var record = match.Record;
            results.Add(new AppVector.VectorSearchResult
            {
                Id = record.Id,
                Score = match.Score,
                Payload = record.Metadata ?? new Dictionary<string, object>()
            });
        }

        return results;
    }
    
    public Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default)
    {
        // Bipins.AI vector stores typically handle collection creation implicitly.
        // For now we assume the collection will be created on first upsert.
        return Task.FromResult(true);
    }
    
    public Task CreateCollectionAsync(string collection, int vectorSize, CancellationToken cancellationToken = default)
    {
        // No-op: underlying Bipins.AI vector store is responsible for creating collections.
        return Task.CompletedTask;
    }
}
