using Bipins.AI.Trading.Application.Contracts;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Domain.ValueObjects;
using Bipins.AI.Trading.Application.Repositories;
using MassTransit;

namespace Bipins.AI.Trading.Application.Consumers;

public class OrderFilledConsumer : IConsumer<OrderFilled>
{
    private readonly IFillRepository _fillRepository;
    private readonly IPortfolioService _portfolioService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderFilledConsumer> _logger;
    
    public OrderFilledConsumer(
        IFillRepository fillRepository,
        IPortfolioService portfolioService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderFilledConsumer> logger)
    {
        _fillRepository = fillRepository;
        _portfolioService = portfolioService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<OrderFilled> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming OrderFilled: {FillId} for order {OrderId}",
            message.FillId, message.OrderId);
        
        // Store fill
        var fill = new Fill
        {
            Id = message.FillId,
            OrderId = message.OrderId,
            ClientOrderId = message.ClientOrderId,
            Symbol = new Symbol(message.Symbol),
            Side = Enum.Parse<OrderSide>(message.Side),
            Quantity = new Quantity(message.Quantity),
            Price = new Money(message.Price, "USD"),
            Commission = new Money(message.Commission, "USD"),
            FilledAt = message.FilledAt
        };
        
        await _fillRepository.AddAsync(fill, context.CancellationToken);
        
        // Update portfolio
        var portfolio = await _portfolioService.GetCurrentPortfolioAsync(context.CancellationToken);
        await _portfolioService.UpdatePortfolioAsync(portfolio, context.CancellationToken);
        
        // Publish PortfolioUpdated
        await _publishEndpoint.Publish(new PortfolioUpdated(
            portfolio.Cash.Amount,
            portfolio.Equity.Amount,
            portfolio.BuyingPower.Amount,
            portfolio.UnrealizedPnL.Amount,
            portfolio.RealizedPnL.Amount,
            portfolio.Positions.Count,
            portfolio.LastUpdatedAt,
            message.CorrelationId), context.CancellationToken);
    }
}
