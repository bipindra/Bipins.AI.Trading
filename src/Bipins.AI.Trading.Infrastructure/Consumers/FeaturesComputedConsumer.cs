using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Bipins.AI.Trading.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Consumers;

public class FeaturesComputedConsumer : IConsumer<FeaturesComputed>
{
    private readonly TradingDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<FeaturesComputedConsumer> _logger;
    
    public FeaturesComputedConsumer(
        TradingDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<FeaturesComputedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<FeaturesComputed> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming FeaturesComputed: {Symbol} {Timeframe} at {Timestamp}",
            message.Symbol, message.Timeframe, message.CandleTimestamp);
        
        // Store feature snapshot (idempotent)
        var existing = await _context.FeatureSnapshots
            .FirstOrDefaultAsync(f => f.Symbol == message.Symbol 
                && f.Timeframe == message.Timeframe 
                && f.CandleTimestamp == message.CandleTimestamp, context.CancellationToken);
        
        if (existing == null)
        {
            var snapshot = new FeatureSnapshotEntity
            {
                Symbol = message.Symbol,
                Timeframe = message.Timeframe,
                CandleTimestamp = message.CandleTimestamp
            };
            snapshot.SetFeatures(message.Features);
            
            _context.FeatureSnapshots.Add(snapshot);
            await _context.SaveChangesAsync(context.CancellationToken);
        }
        
        // Decision engine will be triggered by TradingDecisionHostedService
        _logger.LogDebug("Features stored, decision engine will be triggered");
    }
}
