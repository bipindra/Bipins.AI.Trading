using Bipins.AI.Trading.Domain.Entities;

namespace Bipins.AI.Trading.Domain.Events;

public record CandleReceived(Candle Candle) : IDomainEvent;
