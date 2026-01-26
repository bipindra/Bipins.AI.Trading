# Structured Logging with Correlation IDs Implementation

## Overview
Implement structured logging with correlation IDs for request tracing across the entire application. This will enable end-to-end request tracking through HTTP requests, MassTransit messages, and background services.

## Goals
- Add correlation ID middleware for HTTP requests
- Propagate correlation IDs through MassTransit message headers
- Enrich Serilog logs with correlation IDs automatically
- Add correlation IDs to background service operations
- Ensure correlation IDs flow through the entire request pipeline

## Files to Create/Update

### 1. Create Correlation ID Middleware
- **File**: `src/Bipins.AI.Trading.Web/Middleware/CorrelationIdMiddleware.cs`
- **Purpose**: HTTP middleware to generate/read correlation IDs from headers
- **Behavior**:
  - Check for `X-Correlation-Id` header in incoming requests
  - If present, use it; if not, generate new GUID
  - Store in `HttpContext.Items` and `LogContext`
  - Add to response headers

### 2. Create Correlation ID Service
- **File**: `src/Bipins.AI.Trading.Application/Correlation/ICorrelationIdProvider.cs`
- **File**: `src/Bipins.AI.Trading.Application/Correlation/CorrelationIdProvider.cs`
- **Purpose**: Service to get current correlation ID from async context
- **Methods**:
  - `GetCorrelationId()` - Get current correlation ID
  - `SetCorrelationId(string id)` - Set correlation ID (for background services)

### 3. Update Serilog Configuration
- **File**: `src/Bipins.AI.Trading.Web/appsettings.json`
- **Changes**: Add correlation ID to Serilog enrichers
- **File**: `src/Bipins.AI.Trading.Web/Program.cs`
- **Changes**: Configure Serilog to enrich with correlation ID from LogContext

### 4. Update MassTransit Configuration
- **File**: `src/Bipins.AI.Trading.Web/Program.cs`
- **Changes**: 
  - Add correlation ID to message headers when publishing
  - Read correlation ID from message headers in consumers
  - Set correlation ID in LogContext for consumer operations

### 5. Create MassTransit Correlation ID Filters
- **File**: `src/Bipins.AI.Trading.Infrastructure/Messaging/CorrelationIdPublishFilter.cs`
- **File**: `src/Bipins.AI.Trading.Infrastructure/Messaging/CorrelationIdConsumeFilter.cs`
- **Purpose**: MassTransit filters to handle correlation IDs in message pipeline

### 6. Update Background Services
- **Files**:
  - `src/Bipins.AI.Trading.Web/Services/MarketDataIngestionHostedService.cs`
  - `src/Bipins.AI.Trading.Web/Services/FeatureComputeHostedService.cs`
  - `src/Bipins.AI.Trading.Web/Services/TradingDecisionHostedService.cs`
  - `src/Bipins.AI.Trading.Web/Services/PortfolioReconciliationHostedService.cs`
- **Changes**: Generate and set correlation IDs for each operation cycle

### 7. Update Consumers
- **Files**: All consumer classes in `src/Bipins.AI.Trading.Application/Consumers/`
- **Changes**: Extract correlation ID from message context and set in LogContext

### 8. Update Dependency Injection
- **File**: `src/Bipins.AI.Trading.Infrastructure/DependencyInjection.cs`
- **Changes**: Register correlation ID provider and MassTransit filters

### 9. Update Program.cs
- **File**: `src/Bipins.AI.Trading.Web/Program.cs`
- **Changes**:
  - Add correlation ID middleware to pipeline
  - Configure Serilog correlation ID enricher
  - Register correlation ID services

### 10. Create Documentation
- **File**: `docs/09-correlation-ids.md`
- **Purpose**: Document correlation ID implementation and usage

## Implementation Details

### Correlation ID Flow

1. **HTTP Request**:
   - Middleware checks for `X-Correlation-Id` header
   - If missing, generates new GUID
   - Stores in `HttpContext.Items["CorrelationId"]`
   - Adds to Serilog `LogContext`
   - Adds to response header `X-Correlation-Id`

2. **MassTransit Publishing**:
   - Publish filter reads correlation ID from `HttpContext` or `LogContext`
   - Adds to message headers as `CorrelationId`
   - If no correlation ID exists, generates new one

3. **MassTransit Consuming**:
   - Consume filter reads correlation ID from message headers
   - Sets in `LogContext` for consumer operation
   - All logs within consumer include correlation ID

4. **Background Services**:
   - Generate new correlation ID for each operation cycle
   - Set in `LogContext` at start of operation
   - Include in published messages

### Serilog Enrichment

Add correlation ID to all log entries:
```csharp
LogContext.PushProperty("CorrelationId", correlationId);
```

Or use enricher:
```csharp
.Enrich.FromLogContext()
.Enrich.WithProperty("CorrelationId", correlationId)
```

### Message Contracts

Update message contracts to include correlation ID (already present in some):
- Ensure all message contracts have `CorrelationId` property
- Update message publishing to include correlation ID

### Log Output Format

Structured logs will include:
```json
{
  "Timestamp": "2024-01-01T12:00:00Z",
  "Level": "Information",
  "Message": "Published CandleClosed",
  "CorrelationId": "123e4567-e89b-12d3-a456-426614174000",
  "Symbol": "SPY",
  "Properties": { ... }
}
```

## Configuration

No additional configuration needed in appsettings.json. Correlation IDs are automatically handled by middleware and filters.

## Testing Strategy

- Unit tests for correlation ID middleware
- Integration tests for MassTransit correlation ID propagation
- Verify correlation IDs appear in all log entries
- Test correlation ID propagation across HTTP -> MassTransit -> Consumer flow

## Dependencies

- Serilog.Enrichers.Environment (if not already included)
- Existing Serilog setup
- MassTransit filters support

## Success Criteria

- All HTTP requests have correlation IDs
- All MassTransit messages include correlation IDs in headers
- All log entries include correlation ID property
- Correlation IDs propagate through entire request pipeline
- Background services generate correlation IDs for operations
- Correlation IDs can be used to trace requests end-to-end
