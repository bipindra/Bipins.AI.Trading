using Bipins.AI.Trading.Application.LLM;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using System.Text.Json;

namespace Bipins.AI.Trading.Application.Agents;

public class AgentMemory
{
    private readonly IVectorMemoryStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly ILogger<AgentMemory> _logger;
    private const string CollectionName = "trading_scenarios";
    private const int VectorSize = 1536; // OpenAI text-embedding-3-small dimension
    
    public AgentMemory(
        IVectorMemoryStore vectorStore,
        ILLMService llmService,
        ILogger<AgentMemory> logger)
    {
        _vectorStore = vectorStore;
        _llmService = llmService;
        _logger = logger;
    }
    
    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _vectorStore.CollectionExistsAsync(CollectionName, cancellationToken);
        if (!exists)
        {
            await _vectorStore.CreateCollectionAsync(CollectionName, VectorSize, cancellationToken);
            _logger.LogInformation("Created vector collection: {Collection}", CollectionName);
        }
    }
    
    public async Task StoreScenarioAsync(
        Symbol symbol,
        Dictionary<string, object> marketConditions,
        TradeDecision decision,
        decimal? outcomePnL = null,
        bool? wasSuccessful = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureCollectionExistsAsync(cancellationToken);
            
            // Create embedding text from market conditions and decision
            var embeddingText = CreateEmbeddingText(symbol, marketConditions, decision);
            
            // Generate embedding
            var embedding = await _llmService.GenerateEmbeddingVectorAsync(embeddingText, cancellationToken);
            
            if (embedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for scenario");
                return;
            }
            
            // Create document
            var document = new VectorDocument
            {
                Id = decision.Id ?? Guid.NewGuid().ToString(),
                Vector = embedding,
                Payload = new Dictionary<string, object>
                {
                    ["symbol"] = symbol.Value,
                    ["timestamp"] = decision.CandleTimestamp.ToString("O"),
                    ["decision"] = decision.Action.ToString(),
                    ["confidence"] = decision.Confidence,
                    ["rationale"] = decision.Rationale,
                    ["marketConditions"] = JsonSerializer.Serialize(marketConditions),
                    ["outcomePnL"] = outcomePnL ?? 0m,
                    ["wasSuccessful"] = wasSuccessful?.ToString() ?? "unknown"
                }
            };
            
            await _vectorStore.UpsertAsync(CollectionName, document, cancellationToken);
            _logger.LogDebug("Stored trading scenario {Id} for {Symbol}", document.Id, symbol.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store trading scenario");
        }
    }
    
    public async Task<List<TradingScenario>> SearchSimilarScenariosAsync(
        Symbol symbol,
        Dictionary<string, object> marketConditions,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureCollectionExistsAsync(cancellationToken);
            
            // Create embedding text from current market conditions
            var embeddingText = CreateEmbeddingTextForSearch(symbol, marketConditions);
            
            // Generate embedding
            var embedding = await _llmService.GenerateEmbeddingVectorAsync(embeddingText, cancellationToken);
            
            if (embedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for search");
                return new List<TradingScenario>();
            }
            
            // Search vector store
            var filter = new Dictionary<string, object>
            {
                ["symbol"] = symbol.Value
            };
            
            var results = await _vectorStore.SearchAsync(CollectionName, embedding, topK, filter, cancellationToken);
            
            return results.Select(r => new TradingScenario
            {
                Id = r.Id,
                Symbol = r.Payload.GetValueOrDefault("symbol")?.ToString() ?? string.Empty,
                Timestamp = DateTime.TryParse(r.Payload.GetValueOrDefault("timestamp")?.ToString(), out var ts) ? ts : DateTime.UtcNow,
                Decision = r.Payload.GetValueOrDefault("decision")?.ToString() ?? string.Empty,
                Confidence = decimal.TryParse(r.Payload.GetValueOrDefault("confidence")?.ToString(), out var conf) ? conf : 0m,
                Rationale = r.Payload.GetValueOrDefault("rationale")?.ToString() ?? string.Empty,
                OutcomePnL = decimal.TryParse(r.Payload.GetValueOrDefault("outcomePnL")?.ToString(), out var pnl) ? pnl : null,
                WasSuccessful = bool.TryParse(r.Payload.GetValueOrDefault("wasSuccessful")?.ToString(), out var success) ? success : null,
                SimilarityScore = r.Score,
                MarketConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    r.Payload.GetValueOrDefault("marketConditions")?.ToString() ?? "{}") ?? new Dictionary<string, object>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search similar scenarios");
            return new List<TradingScenario>();
        }
    }
    
    private static string CreateEmbeddingText(Symbol symbol, Dictionary<string, object> marketConditions, TradeDecision decision)
    {
        var parts = new List<string>
        {
            $"Symbol: {symbol.Value}",
            $"Decision: {decision.Action}",
            $"Confidence: {decision.Confidence:F2}",
            $"Rationale: {decision.Rationale}"
        };
        
        if (marketConditions.Any())
        {
            parts.Add("Market Conditions:");
            foreach (var kvp in marketConditions)
            {
                parts.Add($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        if (decision.Features.Any())
        {
            parts.Add("Features:");
            foreach (var kvp in decision.Features)
            {
                parts.Add($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        return string.Join("\n", parts);
    }
    
    private static string CreateEmbeddingTextForSearch(Symbol symbol, Dictionary<string, object> marketConditions)
    {
        var parts = new List<string>
        {
            $"Symbol: {symbol.Value}",
            "Market Conditions:"
        };
        
        foreach (var kvp in marketConditions)
        {
            parts.Add($"  {kvp.Key}: {kvp.Value}");
        }
        
        return string.Join("\n", parts);
    }
}
