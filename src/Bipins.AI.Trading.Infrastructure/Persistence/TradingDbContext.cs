using Bipins.AI.Trading.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bipins.AI.Trading.Infrastructure.Persistence;

public class TradingDbContext : IdentityDbContext<ApplicationUser>
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }
    
    public DbSet<CandleEntity> Candles { get; set; } = null!;
    public DbSet<TickEntity> Ticks { get; set; } = null!;
    public DbSet<FeatureSnapshotEntity> FeatureSnapshots { get; set; } = null!;
    public DbSet<TradeDecisionEntity> TradeDecisions { get; set; } = null!;
    public DbSet<OrderEntity> Orders { get; set; } = null!;
    public DbSet<FillEntity> Fills { get; set; } = null!;
    public DbSet<PortfolioSnapshotEntity> PortfolioSnapshots { get; set; } = null!;
    public DbSet<AgentEventEntity> AgentEvents { get; set; } = null!;
    public DbSet<StrategyEntity> Strategies { get; set; } = null!;
    public DbSet<IndicatorAlertEntity> IndicatorAlerts { get; set; } = null!;
    public DbSet<AlertConditionEntity> AlertConditions { get; set; } = null!;
    public DbSet<ConfigurationEntity> Configurations { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // CandleEntity
        modelBuilder.Entity<CandleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Symbol, e.Timeframe, e.Timestamp }).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timeframe).IsRequired().HasMaxLength(10);
        });
        
        // TickEntity
        modelBuilder.Entity<TickEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
        });
        
        // FeatureSnapshotEntity
        modelBuilder.Entity<FeatureSnapshotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Symbol, e.Timeframe, e.CandleTimestamp }).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timeframe).IsRequired().HasMaxLength(10);
        });
        
        // TradeDecisionEntity
        modelBuilder.Entity<TradeDecisionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Symbol, e.Timeframe, e.CandleTimestamp }).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timeframe).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Rationale).HasMaxLength(1000);
        });
        
        // OrderEntity
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientOrderId).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
        });
        
        // FillEntity
        modelBuilder.Entity<FillEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
        });
        
        // PortfolioSnapshotEntity
        modelBuilder.Entity<PortfolioSnapshotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SnapshotTimestamp);
        });
        
        // AgentEventEntity
        modelBuilder.Entity<AgentEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventTimestamp);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        });
        
        // StrategyEntity
        modelBuilder.Entity<StrategyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StrategyId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Timeframe).IsRequired().HasMaxLength(10);
            entity.Property(e => e.FinalAction).HasMaxLength(10);
        });
        
        // IndicatorAlertEntity
        modelBuilder.Entity<IndicatorAlertEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StrategyId);
            entity.HasIndex(e => e.AlertId).IsUnique();
            entity.Property(e => e.IndicatorType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConditionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timeframe).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(20);
        });
        
        // AlertConditionEntity
        modelBuilder.Entity<AlertConditionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StrategyId);
            entity.HasIndex(e => e.ConditionId).IsUnique();
            entity.Property(e => e.Operator).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(20);
        });
        
        // ConfigurationEntity
        modelBuilder.Entity<ConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
