using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Application.Ports;
using Bipins.AI.Trading.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Trading.Application.Services;

public class RiskManager : IRiskManager
{
    private readonly RiskOptions _options;
    private readonly ILogger<RiskManager> _logger;
    
    public RiskManager(IOptions<RiskOptions> options, ILogger<RiskManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<RiskCheckResult> CheckTradeAsync(TradeDecision decision, Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        
        // Check max position percent
        if (decision.Action != TradeAction.Hold)
        {
            var positionPercent = await GetPositionPercentAsync(decision.Symbol, portfolio, cancellationToken);
            var newPositionPercent = positionPercent + (decision.QuantityPercent ?? 0);
            
            if (newPositionPercent > (decimal)_options.MaxPositionPercent)
            {
                return new RiskCheckResult
                {
                    IsAllowed = false,
                    Reason = $"Position would exceed max position percent ({_options.MaxPositionPercent}%). Current: {positionPercent:F2}%, Proposed: {newPositionPercent:F2}%"
                };
            }
            
            if (newPositionPercent > (decimal)(_options.MaxPositionPercent * 0.8))
            {
                warnings.Add($"Position approaching max limit ({newPositionPercent:F2}% of {_options.MaxPositionPercent}%)");
            }
        }
        
        // Check max open positions
        var openPositions = portfolio.Positions.Count(p => !p.IsFlat);
        if (decision.Action != TradeAction.Hold && openPositions >= _options.MaxOpenPositions)
        {
            var existingPosition = portfolio.GetPosition(decision.Symbol);
            if (existingPosition == null || existingPosition.IsFlat)
            {
                return new RiskCheckResult
                {
                    IsAllowed = false,
                    Reason = $"Max open positions limit reached ({_options.MaxOpenPositions})"
                };
            }
        }
        
        // Check daily loss limit
        var dailyLossCheck = await CheckDailyLossLimitAsync(portfolio, cancellationToken);
        if (!dailyLossCheck)
        {
            return new RiskCheckResult
            {
                IsAllowed = false,
                Reason = $"Daily loss limit exceeded ({_options.MaxDailyLossPercent}%)"
            };
        }
        
        return new RiskCheckResult
        {
            IsAllowed = true,
            Warnings = warnings
        };
    }
    
    public async Task<RiskCheckResult> CheckOrderAsync(Order order, Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        
        // Check position size
        var position = portfolio.GetPosition(order.Symbol);
        var currentPositionPercent = position != null && !position.IsFlat
            ? await GetPositionPercentAsync(order.Symbol, portfolio, cancellationToken)
            : 0;
        
        var orderValue = order.Quantity.Value * (order.LimitPrice?.Amount ?? portfolio.GetPosition(order.Symbol)?.CurrentPrice.Amount ?? 0);
        var portfolioValue = portfolio.Equity.Amount;
        var orderPercent = portfolioValue > 0 ? (orderValue / portfolioValue) * 100 : 0;
        var newPositionPercent = currentPositionPercent + orderPercent;
        
        if (newPositionPercent > (decimal)_options.MaxPositionPercent)
        {
            return new RiskCheckResult
            {
                IsAllowed = false,
                Reason = $"Order would exceed max position percent ({_options.MaxPositionPercent}%)"
            };
        }
        
        // Check buying power
        if (order.Side == OrderSide.Buy && orderValue > portfolio.BuyingPower.Amount)
        {
            return new RiskCheckResult
            {
                IsAllowed = false,
                Reason = $"Insufficient buying power. Required: {orderValue:C}, Available: {portfolio.BuyingPower.Amount:C}"
            };
        }
        
        return new RiskCheckResult
        {
            IsAllowed = true,
            Warnings = warnings
        };
    }
    
    public Task<bool> CheckDailyLossLimitAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        if (portfolio.Equity.IsZero || portfolio.Cash.IsZero)
            return Task.FromResult(true);
        
        // Simplified: check if total PnL is below threshold
        // In production, this would track daily PnL separately
        var lossPercent = portfolio.TotalPnL.Amount < 0
            ? Math.Abs(portfolio.TotalPnL.Amount / portfolio.Cash.Amount) * 100
            : 0;
        
        var isAllowed = lossPercent < (decimal)_options.MaxDailyLossPercent;
        
        if (!isAllowed)
        {
            _logger.LogWarning("Daily loss limit breached: {LossPercent:F2}% (limit: {Limit}%)", lossPercent, _options.MaxDailyLossPercent);
        }
        
        return Task.FromResult(isAllowed);
    }
    
    public Task<bool> CheckMaxPositionsAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        var openPositions = portfolio.Positions.Count(p => !p.IsFlat);
        var isAllowed = openPositions < _options.MaxOpenPositions;
        
        if (!isAllowed)
        {
            _logger.LogWarning("Max positions limit reached: {Count} (limit: {Limit})", openPositions, _options.MaxOpenPositions);
        }
        
        return Task.FromResult(isAllowed);
    }
    
    private Task<decimal> GetPositionPercentAsync(Domain.ValueObjects.Symbol symbol, Portfolio portfolio, CancellationToken cancellationToken)
    {
        var position = portfolio.GetPosition(symbol);
        if (position == null || position.IsFlat || portfolio.Equity.IsZero)
            return Task.FromResult(0m);
        
        var positionValue = Math.Abs(position.MarketValue.Amount);
        var percent = (positionValue / portfolio.Equity.Amount) * 100;
        return Task.FromResult(percent);
    }
}
