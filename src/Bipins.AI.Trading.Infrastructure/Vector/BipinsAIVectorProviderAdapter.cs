// Factory to create Bipins.AI vector store providers from configuration
// QdrantVectorStore should be available from Bipins.AI NuGet package
// TODO: Update namespace once confirmed from NuGet package

using AppOptions = Bipins.AI.Trading.Application.Options;

namespace Bipins.AI.Trading.Infrastructure.Vector;

public static class BipinsAIVectorProviderAdapter
{
    // This will be updated once we confirm the correct namespace from Bipins.AI NuGet package
    // For now, using dynamic to allow compilation while namespace is confirmed
    public static dynamic CreateProvider(AppOptions.VectorDbOptions options, ILoggerFactory loggerFactory)
    {
        // Get the Bipins.AI assembly
        var bipinsAssembly = typeof(Bipins.AI.LLM.ILLMProvider).Assembly;
        
        return options.Provider switch
        {
            "Qdrant" => CreateQdrantVectorStoreFromPackage(bipinsAssembly, options, loggerFactory),
            
            _ => throw new NotSupportedException($"Vector store provider '{options.Provider}' is not supported. Supported providers: Qdrant")
        };
    }
    
    private static object CreateQdrantVectorStoreFromPackage(System.Reflection.Assembly bipinsAssembly, AppOptions.VectorDbOptions options, ILoggerFactory loggerFactory)
    {
        // Find QdrantVectorStore type in Bipins.AI assembly
        var qdrantVectorStoreType = bipinsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "QdrantVectorStore" && t.IsPublic);
        
        if (qdrantVectorStoreType == null)
        {
            throw new InvalidOperationException(
                "QdrantVectorStore not found in Bipins.AI NuGet package. " +
                "Please ensure you have the latest version (1.0.2 or higher) that includes Vector store support.");
        }
        
        // Find QdrantOptions type
        var qdrantOptionsType = bipinsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "QdrantOptions" && t.IsPublic);
        
        if (qdrantOptionsType == null)
        {
            throw new InvalidOperationException("QdrantOptions not found in Bipins.AI NuGet package.");
        }
        
        // Create QdrantOptions instance
        var qdrantOptions = Activator.CreateInstance(qdrantOptionsType);
        var endpointProperty = qdrantOptionsType.GetProperty("Endpoint");
        if (endpointProperty != null)
        {
            endpointProperty.SetValue(qdrantOptions, options.Qdrant.Endpoint);
        }
        
        // Create logger
        var loggerType = typeof(ILogger<>).MakeGenericType(qdrantVectorStoreType);
        var logger = loggerFactory.CreateLogger(qdrantVectorStoreType);
        
        // Find constructor: QdrantVectorStore(QdrantOptions, ILogger<QdrantVectorStore>)
        var constructor = qdrantVectorStoreType.GetConstructor(new[] { qdrantOptionsType, loggerType });
        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"QdrantVectorStore constructor not found with expected parameters (QdrantOptions, ILogger<QdrantVectorStore>).");
        }
        
        return constructor.Invoke(new object[] { qdrantOptions, logger });
    }
}
