using Bipins.AI.Trading.Application.LLM;

namespace Bipins.AI.Trading.Application.Agents;

public static class AgentTools
{
    public static List<FunctionDefinition> GetTradingTools()
    {
        return new List<FunctionDefinition>
        {
            new()
            {
                Name = "get_market_data",
                Description = "Get historical candles and current price for a symbol. Returns price data including open, high, low, close, volume, and timestamps.",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["symbol"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The trading symbol (e.g., 'SPY', 'AAPL')"
                        },
                        ["timeframe"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The timeframe for candles (e.g., '5m', '1h', '1d')",
                            ["enum"] = new[] { "1m", "5m", "15m", "1h", "1d" }
                        },
                        ["from"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Start date in ISO 8601 format (e.g., '2024-01-01T00:00:00Z')"
                        },
                        ["to"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "End date in ISO 8601 format (e.g., '2024-01-02T00:00:00Z')"
                        }
                    },
                    ["required"] = new[] { "symbol", "timeframe" }
                }
            },
            new()
            {
                Name = "get_portfolio_status",
                Description = "Get current portfolio status including cash, equity, buying power, positions, and PnL.",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>(),
                    ["required"] = Array.Empty<string>()
                }
            },
            new()
            {
                Name = "calculate_indicators",
                Description = "Calculate technical indicators (RSI, MACD, Stochastic) for a symbol and timeframe. Returns indicator values and metadata.",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["symbol"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The trading symbol (e.g., 'SPY', 'AAPL')"
                        },
                        ["timeframe"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The timeframe for indicators (e.g., '5m', '1h', '1d')",
                            ["enum"] = new[] { "1m", "5m", "15m", "1h", "1d" }
                        },
                        ["indicators"] = new Dictionary<string, object>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["enum"] = new[] { "RSI", "MACD", "Stochastic" }
                            },
                            ["description"] = "List of indicators to calculate"
                        }
                    },
                    ["required"] = new[] { "symbol", "timeframe", "indicators" }
                }
            },
            new()
            {
                Name = "search_similar_scenarios",
                Description = "Search for similar past trading scenarios from memory. Returns scenarios with similar market conditions, their decisions, and outcomes.",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["symbol"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The trading symbol to search for"
                        },
                        ["marketConditions"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "JSON string describing current market conditions (indicators, price action, etc.)"
                        },
                        ["topK"] = new Dictionary<string, object>
                        {
                            ["type"] = "integer",
                            ["description"] = "Number of similar scenarios to retrieve (default: 5)",
                            ["default"] = 5
                        }
                    },
                    ["required"] = new[] { "symbol", "marketConditions" }
                }
            },
            new()
            {
                Name = "get_trading_history",
                Description = "Get past trading decisions and their outcomes for a symbol. Returns decision history with results and PnL.",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["symbol"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The trading symbol (optional, if not provided returns all symbols)"
                        },
                        ["from"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Start date in ISO 8601 format (optional)"
                        },
                        ["to"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "End date in ISO 8601 format (optional)"
                        },
                        ["limit"] = new Dictionary<string, object>
                        {
                            ["type"] = "integer",
                            ["description"] = "Maximum number of records to return (default: 20)",
                            ["default"] = 20
                        }
                    },
                    ["required"] = Array.Empty<string>()
                }
            }
        };
    }
}
