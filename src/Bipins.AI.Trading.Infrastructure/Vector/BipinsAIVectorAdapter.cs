// Adapter to bridge Bipins.AI vector store (from NuGet package) to Application.Ports.IVectorMemoryStore
// Uses reflection to work with Bipins.AI.Vector types from the NuGet package

using AppVector = Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Ports;

namespace Bipins.AI.Trading.Infrastructure.Vector;

public class BipinsAIVectorAdapter : AppVector.IVectorMemoryStore
{
    private readonly object _bipinsVectorStore;
    private readonly System.Reflection.MethodInfo _upsertMethod;
    private readonly System.Reflection.MethodInfo _searchMethod;
    private readonly System.Reflection.MethodInfo _collectionExistsMethod;
    private readonly System.Reflection.MethodInfo _createCollectionMethod;
    
    public BipinsAIVectorAdapter(object bipinsVectorStore)
    {
        _bipinsVectorStore = bipinsVectorStore ?? throw new ArgumentNullException(nameof(bipinsVectorStore));
        var storeType = bipinsVectorStore.GetType();
        
        // Get methods from IVectorStore interface
        var interfaceType = storeType.GetInterfaces()
            .FirstOrDefault(i => i.Name == "IVectorStore");
        
        if (interfaceType == null)
        {
            throw new InvalidOperationException("The provided object does not implement IVectorStore interface.");
        }
        
        _upsertMethod = interfaceType.GetMethod("UpsertAsync") 
            ?? throw new InvalidOperationException("UpsertAsync method not found");
        _searchMethod = interfaceType.GetMethod("SearchAsync")
            ?? throw new InvalidOperationException("SearchAsync method not found");
        _collectionExistsMethod = interfaceType.GetMethod("CollectionExistsAsync")
            ?? throw new InvalidOperationException("CollectionExistsAsync method not found");
        _createCollectionMethod = interfaceType.GetMethod("CreateCollectionAsync")
            ?? throw new InvalidOperationException("CreateCollectionAsync method not found");
    }
    
    public static IVectorMemoryStore CreateAdapter(object bipinsVectorStore)
    {
        return new BipinsAIVectorAdapter(bipinsVectorStore);
    }
    
    public async Task UpsertAsync(string collection, AppVector.VectorDocument document, CancellationToken cancellationToken = default)
    {
        // Create Bipins.AI.Vector.VectorDocument using reflection
        var bipinsDocType = typeof(Bipins.AI.LLM.ILLMProvider).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "VectorDocument" && t.Namespace?.Contains("Vector") == true);
        
        if (bipinsDocType == null)
        {
            throw new InvalidOperationException("VectorDocument type not found in Bipins.AI package.");
        }
        
        var bipinsDoc = Activator.CreateInstance(bipinsDocType);
        bipinsDocType.GetProperty("Id")?.SetValue(bipinsDoc, document.Id);
        bipinsDocType.GetProperty("Vector")?.SetValue(bipinsDoc, document.Vector);
        bipinsDocType.GetProperty("Payload")?.SetValue(bipinsDoc, document.Payload);
        
        var task = (Task)_upsertMethod.Invoke(_bipinsVectorStore, new object[] { collection, bipinsDoc, cancellationToken })!;
        await task;
    }
    
    public async Task<List<AppVector.VectorSearchResult>> SearchAsync(
        string collection,
        float[] vector,
        int topK = 10,
        Dictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var task = (Task)_searchMethod.Invoke(_bipinsVectorStore, new object[] { collection, vector, topK, filter, cancellationToken })!;
        await task;
        
        var resultProperty = task.GetType().GetProperty("Result");
        var results = resultProperty?.GetValue(task) as System.Collections.IEnumerable;
        
        if (results == null)
        {
            return new List<AppVector.VectorSearchResult>();
        }
        
        var searchResultType = typeof(Bipins.AI.LLM.ILLMProvider).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "VectorSearchResult" && t.Namespace?.Contains("Vector") == true);
        
        if (searchResultType == null)
        {
            throw new InvalidOperationException("VectorSearchResult type not found in Bipins.AI package.");
        }
        
        var list = new List<AppVector.VectorSearchResult>();
        foreach (var result in results)
        {
            var id = searchResultType.GetProperty("Id")?.GetValue(result)?.ToString() ?? string.Empty;
            var score = (float)(searchResultType.GetProperty("Score")?.GetValue(result) ?? 0f);
            var payload = searchResultType.GetProperty("Payload")?.GetValue(result) as Dictionary<string, object> 
                ?? new Dictionary<string, object>();
            
            list.Add(new AppVector.VectorSearchResult
            {
                Id = id,
                Score = score,
                Payload = payload
            });
        }
        
        return list;
    }
    
    public async Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default)
    {
        var task = (Task<bool>)_collectionExistsMethod.Invoke(_bipinsVectorStore, new object[] { collection, cancellationToken })!;
        return await task;
    }
    
    public async Task CreateCollectionAsync(string collection, int vectorSize, CancellationToken cancellationToken = default)
    {
        var task = (Task)_createCollectionMethod.Invoke(_bipinsVectorStore, new object[] { collection, vectorSize, cancellationToken })!;
        await task;
    }
}
