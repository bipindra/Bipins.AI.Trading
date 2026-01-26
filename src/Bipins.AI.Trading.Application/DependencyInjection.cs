using Bipins.AI.Trading.Application.Consumers;
using Bipins.AI.Trading.Application.Indicators;
using Bipins.AI.Trading.Application.Infrastructure;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Application.Services;
using Bipins.AI.Trading.Application.Strategies;
using Bipins.AI.Trading.Application.TickData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Trading.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register clock
        services.AddSingleton<IClock, Clock>();
        
        // Register services
        services.AddScoped<IRiskManager, RiskManager>();
        
        // Register AI Agent components (always register, they will handle LLM configuration internally)
        services.AddScoped<Agents.AgentMemory>();
        services.AddScoped<Agents.TradingAgent>();
        
        // Register decision engines - AI Agent takes precedence if LLM is configured
        var llmOptions = new Options.LLMOptions();
        configuration.GetSection(Options.LLMOptions.SectionName).Bind(llmOptions);
        
        if (llmOptions != null && !string.IsNullOrEmpty(llmOptions.Provider) && 
            ((llmOptions.Provider == "OpenAI" && !string.IsNullOrEmpty(llmOptions.OpenAI.ApiKey)) ||
             (llmOptions.Provider == "Anthropic" && !string.IsNullOrEmpty(llmOptions.Anthropic.ApiKey)) ||
             (llmOptions.Provider == "AzureOpenAI" && !string.IsNullOrEmpty(llmOptions.AzureOpenAI.ApiKey))))
        {
            // Use AI Agent decision engine if LLM is configured
            services.AddScoped<IDecisionEngine, AIAgentDecisionEngine>();
            services.AddScoped<DecisionEngine>(); // Keep for fallback
        }
        else
        {
            // Use deterministic decision engine if LLM not configured
            services.AddScoped<IDecisionEngine, DecisionEngine>();
        }
        
        services.AddScoped<IOrderExecutionPolicy, OrderExecutionPolicy>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        
        // Register indicator system
        services.AddSingleton<IndicatorRegistry>(sp =>
        {
            var registry = new IndicatorRegistry();
            registry.Register<MACDResult>(new MACDCalculator());
            registry.Register<RSIResult>(new RSICalculator());
            registry.Register<StochasticResult>(new StochasticCalculator());
            return registry;
        });
        services.AddScoped<IndicatorService>();
        
        // Register strategy system
        services.AddScoped<IndicatorHistoryService>();
        services.AddScoped<AlertEvaluator>();
        services.AddScoped<ConditionCombiner>();
        services.AddScoped<StrategyExecutor>();
        
        // Register tick data services
        services.AddScoped<TickAggregator>();
        services.AddScoped<TickDataService>();
        
        // Register consumers
        services.AddScoped<CandleClosedConsumer>();
        services.AddScoped<IndicatorsCalculatedConsumer>();
        services.AddScoped<TradeProposedConsumer>();
        services.AddScoped<TradeApprovedConsumer>();
        services.AddScoped<OrderFilledConsumer>();
        
        return services;
    }
}
