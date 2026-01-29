// Factory to resolve Bipins.AI vector store from dependency injection container
// Vector stores are registered via IBipinsAIBuilder.AddQdrant/AddPinecone/etc in DependencyInjection

using Bipins.AI.Vector;
using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Trading.Infrastructure.Vector;

public class VectorStoreFactory
{
    private readonly IServiceProvider _serviceProvider;

    public VectorStoreFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IVectorStore GetVectorStore()
    {
        // Resolve IVectorStore from DI container (registered via IBipinsAIBuilder)
        return _serviceProvider.GetRequiredService<IVectorStore>();
    }
}
