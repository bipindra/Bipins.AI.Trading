using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Repositories;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using MassTransit;

namespace Bipins.AI.Trading.Application.Consumers;

public class CandleClosedConsumer : IConsumer<CandleClosed>
{
    private readonly ICandleRepository _candleRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CandleClosedConsumer> _logger;
    
    public CandleClosedConsumer(
        ICandleRepository candleRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<CandleClosedConsumer> logger)
    {
        _candleRepository = candleRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<CandleClosed> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming CandleClosed: {Symbol} {Timeframe} at {Timestamp}",
            message.Symbol, message.Timeframe, message.Timestamp);
        
        // Convert to domain entity
        var candle = new Candle
        {
            Symbol = new Symbol(message.Symbol),
            Timeframe = new Timeframe(message.Timeframe),
            Timestamp = message.Timestamp,
            Open = message.Open,
            High = message.High,
            Low = message.Low,
            Close = message.Close,
            Volume = message.Volume
        };
        
        // Store candle (idempotent)
        await _candleRepository.AddAsync(candle, context.CancellationToken);
        
        // Trigger feature computation
        // This will be handled by FeatureComputeHostedService which subscribes to CandleClosed
        _logger.LogDebug("Candle stored, feature computation will be triggered");
    }
}
