using Bipins.AI.Trading.Application.Agents;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;

namespace Bipins.AI.Trading.Application.Services;

public class AIAgentDecisionEngine : IDecisionEngine
{
    private readonly TradingAgent _tradingAgent;
    private readonly ILogger<AIAgentDecisionEngine> _logger;
    
    public AIAgentDecisionEngine(TradingAgent tradingAgent, ILogger<AIAgentDecisionEngine> logger)
    {
        _tradingAgent = tradingAgent;
        _logger = logger;
    }
    
    public async Task<TradeDecision> MakeDecisionAsync(
        Symbol symbol,
        Timeframe timeframe,
        List<Candle> candles,
        Dictionary<string, decimal> features,
        Portfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI Agent making decision for {Symbol} on {Timeframe}", symbol.Value, timeframe.Value);
            
            // Build agent context
            var context = await _tradingAgent.BuildContextAsync(symbol, timeframe, cancellationToken);
            
            // Override with provided data if available
            if (candles.Any())
            {
                context.Candles = candles;
            }
            if (features.Any())
            {
                context.Features = features;
            }
            context.Portfolio = portfolio;
            
            // Make decision using AI agent
            var decision = await _tradingAgent.MakeDecisionAsync(context, cancellationToken);
            
            _logger.LogInformation(
                "AI Agent decision for {Symbol}: {Action} with confidence {Confidence:F2} - {Rationale}",
                symbol.Value, decision.Action, decision.Confidence, decision.Rationale);
            
            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI Agent decision engine for {Symbol}", symbol.Value);
            
            // Fallback to Hold on error
            return new TradeDecision
            {
                Symbol = symbol,
                Timeframe = timeframe,
                CandleTimestamp = candles.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Action = TradeAction.Hold,
                Confidence = 0.0m,
                Rationale = $"AI Agent error: {ex.Message}",
                Features = features.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
            };
        }
    }
}
