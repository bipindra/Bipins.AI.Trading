# Operations Guide

## Trading Modes

### Ask Mode

In Ask mode, every trade proposal requires manual approval:

1. Decision engine proposes trade
2. Risk manager validates
3. ActionRequired message published
4. Portal shows pending action
5. User approves/rejects
6. If approved, order submitted

**Use Ask mode when:**
- Testing new strategies
- Learning the system
- Conservative trading
- Need full control

### Auto Mode

In Auto mode, trades execute automatically after risk checks:

1. Decision engine proposes trade
2. Risk manager validates
3. If passed, automatically approved
4. Order submitted immediately

**Use Auto mode when:**
- Strategy is proven
- Comfortable with risk limits
- Want fully automated trading

## Kill Switch

Trading can be disabled via:

1. **Configuration**: Set `Trading.Enabled = false` in appsettings.json
2. **Portal**: Trading Control page (requires restart)
3. **Automatic**: Risk breach triggers automatic disable

**Important**: Configuration changes require application restart.

## Monitoring

### Dashboard

- Real-time portfolio balance
- Trading status (enabled/disabled, mode)
- Last update timestamp
- Position count

### Portfolio Page

- All open positions
- Unrealized P&L
- Cash and equity
- Buying power

### Orders Page

- All orders (pending, filled, rejected)
- Filter by status
- View order details

### Decisions Page

- All trading decisions
- Filter by symbol/date
- Export to CSV
- View confidence and rationale

### Pending Actions Page (Ask Mode)

- All pending approvals
- Approve/Reject buttons
- Decision details

### System Events Page

- Risk breaches
- Feed disconnections
- Errors
- Filter by type/date

## Logging

Logs are written to:
- **Console**: Structured JSON
- **File**: `logs/trading-{date}.log`

Log levels:
- **Information**: Normal operations
- **Warning**: Non-critical issues
- **Error**: Failures requiring attention

## Troubleshooting

### No Candles Received

**Symptoms**: Dashboard shows no data, no decisions

**Causes:**
- Alpaca credentials missing/invalid
- Market closed
- Network issues

**Solutions:**
1. Check Alpaca credentials in appsettings.json
2. Verify market hours
3. Check logs for errors
4. Test Alpaca connection manually

### Trading Not Executing

**Symptoms**: Decisions made but no orders

**Causes:**
- Trading.Enabled = false
- Risk checks failing
- Ask mode waiting approval

**Solutions:**
1. Check Trading.Enabled in config
2. Review System Events for risk breaches
3. Check Pending Actions page
4. Review risk configuration

### Orders Failing

**Symptoms**: Orders submitted but rejected

**Causes:**
- Insufficient buying power
- Invalid order parameters
- Broker API issues

**Solutions:**
1. Check portfolio buying power
2. Review order details in logs
3. Verify Alpaca account status
4. Check broker API status

### Database Issues

**Symptoms**: Data not persisting

**Note**: SQLite in-memory database is reset on restart. This is expected behavior.

**For persistence:**
1. Switch to SQL Server
2. Update connection string
3. Run migrations

## Best Practices

1. **Start with Ask Mode**: Always test in Ask mode first
2. **Monitor Risk Limits**: Review System Events regularly
3. **Check Logs Daily**: Review logs for errors
4. **Test Strategies**: Use paper trading extensively
5. **Backup Configuration**: Keep appsettings.json in version control
6. **Gradual Rollout**: Start with one symbol, expand slowly

## Maintenance

### Daily
- Review dashboard
- Check system events
- Review decisions log

### Weekly
- Review portfolio performance
- Analyze decision quality
- Adjust risk parameters if needed

### Monthly
- Review logs for patterns
- Update strategies if needed
- Backup configuration

## Emergency Procedures

### Stop Trading Immediately

1. Set `Trading.Enabled = false` in appsettings.json
2. Restart application
3. Or: Stop application

### Risk Breach Detected

1. System automatically disables trading
2. Review System Events for details
3. Adjust risk parameters
4. Re-enable only after review

### Broker Connection Lost

1. System logs FeedDisconnected event
2. Trading continues with last known data
3. Restore connection
4. System resumes automatically
