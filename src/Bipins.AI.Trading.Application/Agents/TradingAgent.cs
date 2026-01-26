using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Application.LLM;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using System.Text.Json;

namespace Bipins.AI.Trading.Application.Agents;

public class TradingAgent
{
    private readonly ILLMService _llmService;
    private readonly IMarketDataClient _marketDataClient;
    private readonly IPortfolioService _portfolioService;
    private readonly IndicatorService _indicatorService;
    private readonly ICandleRepository _candleRepository;
    private readonly ITradeDecisionRepository _tradeDecisionRepository;
    private readonly AgentMemory _agentMemory;
    private readonly ILogger<TradingAgent> _logger;
    
    public TradingAgent(
        ILLMService llmService,
        IMarketDataClient marketDataClient,
        IPortfolioService portfolioService,
        IndicatorService indicatorService,
        ICandleRepository candleRepository,
        ITradeDecisionRepository tradeDecisionRepository,
        AgentMemory agentMemory,
        ILogger<TradingAgent> logger)
    {
        _llmService = llmService;
        _marketDataClient = marketDataClient;
        _portfolioService = portfolioService;
        _indicatorService = indicatorService;
        _candleRepository = candleRepository;
        _tradeDecisionRepository = tradeDecisionRepository;
        _agentMemory = agentMemory;
        _logger = logger;
    }
    
    public async Task<AgentContext> BuildContextAsync(
        Symbol symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var context = new AgentContext
        {
            Symbol = symbol,
            Timeframe = timeframe,
            Timestamp = DateTime.UtcNow
        };
        
        // Get candles
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        context.Candles = await _candleRepository.GetCandlesAsync(symbol, timeframe, from, to, cancellationToken);
        
        // Get portfolio
        context.Portfolio = await _portfolioService.GetCurrentPortfolioAsync(cancellationToken);
        
        // Calculate indicators
        if (context.Candles.Count >= 14)
        {
            var indicatorNames = new List<string> { "MACD", "RSI", "Stochastic" };
            var indicators = _indicatorService.CalculateAll(indicatorNames, context.Candles);
            
            context.Indicators = indicators.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<string, object>
                {
                    ["Timestamp"] = kvp.Value.Timestamp,
                    ["Metadata"] = kvp.Value.Metadata
                }.Concat(GetIndicatorValues(kvp.Value)).ToDictionary(x => x.Key, x => x.Value)
            );
        }
        
        // Build market conditions for RAG search
        var marketConditions = BuildMarketConditions(context);
        
        // Search similar scenarios
        context.SimilarScenarios = await _agentMemory.SearchSimilarScenariosAsync(
            symbol, marketConditions, topK: 5, cancellationToken);
        
        // Get trading history
        var history = await _tradeDecisionRepository.GetDecisionsAsync(
            symbol.Value, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, cancellationToken);
        context.TradingHistory = history.Take(20).ToList();
        
        return context;
    }
    
    public async Task<TradeDecision> MakeDecisionAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(context);
        var tools = AgentTools.GetTradingTools();
        
        // First, let the agent gather information using tools
        var response = await _llmService.ChatWithFunctionsAsync(
            systemPrompt,
            userPrompt,
            tools.Select(t => new FunctionDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.Parameters
            }).ToList(),
            cancellationToken);
        
        // Execute function calls if any
        var functionResults = new List<string>();
        if (response.FunctionCalls != null && response.FunctionCalls.Any())
        {
            foreach (var functionCall in response.FunctionCalls)
            {
                var result = await ExecuteToolAsync(functionCall, context, cancellationToken);
                functionResults.Add(result);
            }
            
            // Continue conversation with function results
            var followUpPrompt = userPrompt + "\n\nTool Results:\n" + string.Join("\n", functionResults);
            response = await _llmService.ChatWithFunctionsAsync(
                systemPrompt,
                followUpPrompt,
                tools.Select(t => new FunctionDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.Parameters
                }).ToList(),
                cancellationToken);
        }
        
        // Parse decision from LLM response
        var decision = ParseDecisionFromResponse(response, context);
        
        // Store scenario for future learning
        var marketConditions = BuildMarketConditions(context);
        await _agentMemory.StoreScenarioAsync(
            context.Symbol,
            marketConditions,
            decision,
            cancellationToken: cancellationToken);
        
        return decision;
    }
    
    private async Task<string> ExecuteToolAsync(
        FunctionCall functionCall,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall.Arguments) ?? new Dictionary<string, object>();
            
            return functionCall.Name switch
            {
                "get_market_data" => await ExecuteGetMarketData(args, context, cancellationToken),
                "get_portfolio_status" => await ExecuteGetPortfolioStatus(context, cancellationToken),
                "calculate_indicators" => await ExecuteCalculateIndicators(args, context, cancellationToken),
                "search_similar_scenarios" => await ExecuteSearchSimilarScenarios(args, context, cancellationToken),
                "get_trading_history" => await ExecuteGetTradingHistory(args, context, cancellationToken),
                _ => $"Unknown tool: {functionCall.Name}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", functionCall.Name);
            return $"Error executing {functionCall.Name}: {ex.Message}";
        }
    }
    
    private async Task<string> ExecuteGetMarketData(
        Dictionary<string, object> args,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var symbol = args.GetValueOrDefault("symbol")?.ToString() ?? context.Symbol.Value;
        var timeframe = args.GetValueOrDefault("timeframe")?.ToString() ?? context.Timeframe.Value;
        var fromStr = args.GetValueOrDefault("from")?.ToString();
        var toStr = args.GetValueOrDefault("to")?.ToString();
        
        var from = fromStr != null ? DateTime.Parse(fromStr) : DateTime.UtcNow.AddDays(-7);
        var to = toStr != null ? DateTime.Parse(toStr) : DateTime.UtcNow;
        
        var candles = await _candleRepository.GetCandlesAsync(
            new Symbol(symbol), new Timeframe(timeframe), from, to, cancellationToken);
        
        var currentPrice = await _marketDataClient.GetCurrentPriceAsync(new Symbol(symbol), cancellationToken);
        
        var result = new
        {
            symbol = symbol,
            timeframe = timeframe,
            currentPrice = currentPrice,
            candleCount = candles.Count,
            latestCandle = candles.LastOrDefault() != null ? new
            {
                timestamp = candles.Last().Timestamp,
                open = candles.Last().Open,
                high = candles.Last().High,
                low = candles.Last().Low,
                close = candles.Last().Close,
                volume = candles.Last().Volume
            } : null
        };
        
        return JsonSerializer.Serialize(result);
    }
    
    private async Task<string> ExecuteGetPortfolioStatus(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(cancellationToken);
        
        var result = new
        {
            cash = portfolio.Cash.Amount,
            equity = portfolio.Equity.Amount,
            buyingPower = portfolio.BuyingPower.Amount,
            unrealizedPnL = portfolio.UnrealizedPnL.Amount,
            realizedPnL = portfolio.RealizedPnL.Amount,
            positionCount = portfolio.Positions.Count,
            positions = portfolio.Positions.Select(p => new
            {
                symbol = p.Symbol.Value,
                quantity = p.Quantity.Value,
                averagePrice = p.AveragePrice.Amount,
                currentPrice = p.CurrentPrice.Amount,
                unrealizedPnL = p.UnrealizedPnL.Amount
            }).ToList()
        };
        
        return JsonSerializer.Serialize(result);
    }
    
    private async Task<string> ExecuteCalculateIndicators(
        Dictionary<string, object> args,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var symbol = args.GetValueOrDefault("symbol")?.ToString() ?? context.Symbol.Value;
        var timeframe = args.GetValueOrDefault("timeframe")?.ToString() ?? context.Timeframe.Value;
        var indicatorsList = args.GetValueOrDefault("indicators");
        
        var indicatorNames = new List<string>();
        if (indicatorsList is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
        {
            indicatorNames = jsonArray.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        
        if (!indicatorNames.Any())
        {
            indicatorNames = new List<string> { "MACD", "RSI", "Stochastic" };
        }
        
        var candles = await _candleRepository.GetCandlesAsync(
            new Symbol(symbol), new Timeframe(timeframe), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, cancellationToken);
        
        if (candles.Count < 14)
        {
            return JsonSerializer.Serialize(new { error = "Insufficient data for indicator calculation" });
        }
        
        var indicators = _indicatorService.CalculateAll(indicatorNames, candles);
        
        var result = indicators.ToDictionary(
            kvp => kvp.Key,
            kvp => GetIndicatorValues(kvp.Value)
        );
        
        return JsonSerializer.Serialize(result);
    }
    
    private async Task<string> ExecuteSearchSimilarScenarios(
        Dictionary<string, object> args,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var symbol = args.GetValueOrDefault("symbol")?.ToString() ?? context.Symbol.Value;
        var marketConditionsStr = args.GetValueOrDefault("marketConditions")?.ToString() ?? "{}";
        var topK = args.GetValueOrDefault("topK") != null ? Convert.ToInt32(args["topK"]) : 5;
        
        Dictionary<string, object> marketConditions;
        try
        {
            marketConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(marketConditionsStr) ?? new Dictionary<string, object>();
        }
        catch
        {
            marketConditions = BuildMarketConditions(context);
        }
        
        var scenarios = await _agentMemory.SearchSimilarScenariosAsync(
            new Symbol(symbol), marketConditions, topK, cancellationToken);
        
        var result = scenarios.Select(s => new
        {
            id = s.Id,
            symbol = s.Symbol,
            timestamp = s.Timestamp,
            decision = s.Decision,
            confidence = s.Confidence,
            rationale = s.Rationale,
            outcomePnL = s.OutcomePnL,
            wasSuccessful = s.WasSuccessful,
            similarityScore = s.SimilarityScore
        }).ToList();
        
        return JsonSerializer.Serialize(result);
    }
    
    private async Task<string> ExecuteGetTradingHistory(
        Dictionary<string, object> args,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var symbolStr = args.GetValueOrDefault("symbol")?.ToString();
        var fromStr = args.GetValueOrDefault("from")?.ToString();
        var toStr = args.GetValueOrDefault("to")?.ToString();
        var limit = args.GetValueOrDefault("limit") != null ? Convert.ToInt32(args["limit"]) : 20;
        
        var from = fromStr != null ? DateTime.Parse(fromStr) : DateTime.UtcNow.AddDays(-30);
        var to = toStr != null ? DateTime.Parse(toStr) : DateTime.UtcNow;
        
        List<TradeDecision> decisions;
        if (!string.IsNullOrEmpty(symbolStr))
        {
            decisions = await _tradeDecisionRepository.GetDecisionsAsync(
                symbolStr, from, to, cancellationToken);
        }
        else
        {
            decisions = await _tradeDecisionRepository.GetDecisionsAsync(
                null, from, to, cancellationToken);
        }
        
        decisions = decisions.Take(limit).ToList();
        
        var result = decisions.Select(d => new
        {
            id = d.Id,
            symbol = d.Symbol.Value,
            timestamp = d.CandleTimestamp,
            action = d.Action.ToString(),
            confidence = d.Confidence,
            rationale = d.Rationale,
            quantityPercent = d.QuantityPercent
        }).ToList();
        
        return JsonSerializer.Serialize(result);
    }
    
    private Dictionary<string, object> BuildMarketConditions(AgentContext context)
    {
        var conditions = new Dictionary<string, object>();
        
        if (context.Candles.Any())
        {
            var latest = context.Candles.Last();
            conditions["close"] = latest.Close;
            conditions["volume"] = latest.Volume;
            conditions["high"] = latest.High;
            conditions["low"] = latest.Low;
        }
        
        foreach (var indicator in context.Indicators)
        {
            conditions[$"indicator_{indicator.Key}"] = indicator.Value;
        }
        
        return conditions;
    }
    
    private string BuildSystemPrompt()
    {
        return @"You are an autonomous AI trading agent. Your role is to analyze market conditions and make trading decisions (Buy, Sell, or Hold).

Key principles:
1. Always prioritize risk management and capital preservation
2. Use technical indicators (RSI, MACD, Stochastic) to inform decisions
3. Consider portfolio position and diversification
4. Learn from similar past scenarios
5. Provide clear reasoning for your decisions
6. Only recommend trades when confidence is high (>0.6)

You have access to tools to:
- Get market data (candles, prices)
- Check portfolio status
- Calculate technical indicators
- Search similar past trading scenarios
- Review trading history

When making a decision, analyze:
1. Current market conditions and indicators
2. Similar past scenarios and their outcomes
3. Current portfolio positions
4. Risk/reward ratios

Format your decision as JSON with:
- action: 'Buy', 'Sell', or 'Hold'
- confidence: 0.0 to 1.0
- rationale: clear explanation
- quantityPercent: percentage of portfolio (0-100) if action is Buy/Sell
- suggestedStopLoss: optional stop loss price
- suggestedTakeProfit: optional take profit price";
    }
    
    private string BuildUserPrompt(AgentContext context)
    {
        var prompt = new List<string>
        {
            $"Analyze {context.Symbol.Value} on {context.Timeframe.Value} timeframe and make a trading decision.",
            "",
            "Current Market Data:",
            $"- Latest Close: {context.Candles.LastOrDefault()?.Close ?? 0m}",
            $"- Volume: {context.Candles.LastOrDefault()?.Volume ?? 0}",
            ""
        };
        
        if (context.Indicators.Any())
        {
            prompt.Add("Technical Indicators:");
            foreach (var indicator in context.Indicators)
            {
                prompt.Add($"- {indicator.Key}: {JsonSerializer.Serialize(indicator.Value)}");
            }
            prompt.Add("");
        }
        
        if (context.SimilarScenarios.Any())
        {
            prompt.Add($"Similar Past Scenarios ({context.SimilarScenarios.Count}):");
            foreach (var scenario in context.SimilarScenarios.Take(3))
            {
                prompt.Add($"- {scenario.Decision} (confidence: {scenario.Confidence:F2}, PnL: {scenario.OutcomePnL}, similarity: {scenario.SimilarityScore:F3})");
                prompt.Add($"  Rationale: {scenario.Rationale}");
            }
            prompt.Add("");
        }
        
        prompt.Add($"Portfolio Status:");
        prompt.Add($"- Cash: {context.Portfolio.Cash.Amount:C}");
        prompt.Add($"- Equity: {context.Portfolio.Equity.Amount:C}");
        prompt.Add($"- Positions: {context.Portfolio.Positions.Count}");
        prompt.Add("");
        prompt.Add("Use the available tools to gather any additional information you need, then make your trading decision.");
        
        return string.Join("\n", prompt);
    }
    
    private TradeDecision ParseDecisionFromResponse(LLMResponse response, AgentContext context)
    {
        try
        {
            // Try to parse JSON decision from response
            var content = response.Content.Trim();
            
            // Extract JSON if wrapped in markdown code blocks
            if (content.Contains("```json"))
            {
                var start = content.IndexOf("```json") + 7;
                var end = content.IndexOf("```", start);
                if (end > start)
                {
                    content = content.Substring(start, end - start).Trim();
                }
            }
            else if (content.Contains("```"))
            {
                var start = content.IndexOf("```") + 3;
                var end = content.IndexOf("```", start);
                if (end > start)
                {
                    content = content.Substring(start, end - start).Trim();
                }
            }
            
            var decisionJson = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            
            var actionStr = decisionJson?.GetValueOrDefault("action")?.ToString() ?? "Hold";
            var action = Enum.TryParse<TradeAction>(actionStr, true, out var parsedAction) ? parsedAction : TradeAction.Hold;
            
            var confidence = decisionJson?.GetValueOrDefault("confidence") != null
                ? Convert.ToDecimal(decisionJson["confidence"])
                : 0.5m;
            
            var rationale = decisionJson?.GetValueOrDefault("rationale")?.ToString() ?? response.Content;
            
            var quantityPercent = decisionJson?.GetValueOrDefault("quantityPercent") != null
                ? Convert.ToDecimal(decisionJson["quantityPercent"])
                : (decimal?)null;
            
            Money? stopLoss = null;
            Money? takeProfit = null;
            
            if (decisionJson?.GetValueOrDefault("suggestedStopLoss") != null)
            {
                var stopLossValue = Convert.ToDecimal(decisionJson["suggestedStopLoss"]);
                stopLoss = new Money(stopLossValue, "USD");
            }
            
            if (decisionJson?.GetValueOrDefault("suggestedTakeProfit") != null)
            {
                var takeProfitValue = Convert.ToDecimal(decisionJson["suggestedTakeProfit"]);
                takeProfit = new Money(takeProfitValue, "USD");
            }
            
            return new TradeDecision
            {
                Symbol = context.Symbol,
                Timeframe = context.Timeframe,
                CandleTimestamp = context.Candles.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Action = action,
                Confidence = Math.Clamp(confidence, 0m, 1m),
                Rationale = rationale,
                QuantityPercent = quantityPercent,
                SuggestedStopLoss = stopLoss,
                SuggestedTakeProfit = takeProfit,
                Features = BuildMarketConditions(context).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse decision from LLM response, using default");
            
            // Fallback to Hold if parsing fails
            return new TradeDecision
            {
                Symbol = context.Symbol,
                Timeframe = context.Timeframe,
                CandleTimestamp = context.Candles.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Action = TradeAction.Hold,
                Confidence = 0.0m,
                Rationale = $"Failed to parse LLM response: {response.Content}",
                Features = BuildMarketConditions(context).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
    }
    
    private Dictionary<string, object> GetIndicatorValues(IndicatorResult result)
    {
        return result switch
        {
            MACDResult macd => new Dictionary<string, object>
            {
                ["MACD"] = macd.MACD,
                ["Signal"] = macd.Signal,
                ["Histogram"] = macd.Histogram
            },
            RSIResult rsi => new Dictionary<string, object>
            {
                ["Value"] = rsi.Value
            },
            StochasticResult stoch => new Dictionary<string, object>
            {
                ["PercentK"] = stoch.PercentK,
                ["PercentD"] = stoch.PercentD
            },
            _ => new Dictionary<string, object>()
        };
    }
}
