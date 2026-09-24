using MarketPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class MarketPulseDbContext : DbContext
{
    public MarketPulseDbContext(DbContextOptions<MarketPulseDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessedEventRecord> ProcessedEvents => Set<ProcessedEventRecord>();
    public DbSet<MarketEventRecord> MarketEvents => Set<MarketEventRecord>();
    public DbSet<MarketBarRecord> MarketBars => Set<MarketBarRecord>();
    public DbSet<AnomalyRecord> Anomalies => Set<AnomalyRecord>();
    public DbSet<DetectionRuleRecord> DetectionRules => Set<DetectionRuleRecord>();
    public DbSet<AlertConfigurationRecord> AlertConfigurations => Set<AlertConfigurationRecord>();
    public DbSet<AlertDeliveryRecord> AlertDeliveries => Set<AlertDeliveryRecord>();
    public DbSet<BacktestRunRecord> BacktestRuns => Set<BacktestRunRecord>();
    public DbSet<BacktestResultRecord> BacktestResults => Set<BacktestResultRecord>();
    public DbSet<WatchlistRecord> Watchlists => Set<WatchlistRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEventRecord>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Symbol);
        });

        modelBuilder.Entity<MarketEventRecord>(entity =>
        {
            entity.ToTable("MarketEvents");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.Symbol, x.Timestamp });
        });

        modelBuilder.Entity<MarketBarRecord>(entity =>
        {
            entity.ToTable("MarketBars");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Open).HasPrecision(18, 6);
            entity.Property(x => x.High).HasPrecision(18, 6);
            entity.Property(x => x.Low).HasPrecision(18, 6);
            entity.Property(x => x.Close).HasPrecision(18, 6);
            entity.HasIndex(x => new { x.Symbol, x.Timestamp });
        });

        modelBuilder.Entity<AnomalyRecord>(entity =>
        {
            entity.ToTable("Anomalies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RuleVersion).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Score).HasPrecision(6, 1);
            entity.Property(x => x.RelativeVolume).HasPrecision(18, 6);
            entity.Property(x => x.VolumeAcceleration).HasPrecision(18, 6);
            entity.Property(x => x.PriceChangePercent).HasPrecision(18, 6);
            entity.Property(x => x.VwapDeviationPercent).HasPrecision(18, 6);
            entity.Property(x => x.LastPrice).HasPrecision(18, 6);
            entity.HasIndex(x => x.SourceEventId).IsUnique();
            entity.HasIndex(x => new { x.Symbol, x.DetectedAt });
            entity.HasIndex(x => new { x.Severity, x.DetectedAt });
        });

        modelBuilder.Entity<DetectionRuleRecord>(entity =>
        {
            entity.ToTable("DetectionRules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Version).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Version).IsUnique();
        });

        modelBuilder.Entity<AlertConfigurationRecord>(entity =>
        {
            entity.ToTable("AlertConfigurations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.MinSeverity).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<AlertDeliveryRecord>(entity =>
        {
            entity.ToTable("AlertDeliveries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.AnomalyId);
        });

        modelBuilder.Entity<BacktestRunRecord>(entity =>
        {
            entity.ToTable("BacktestRuns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<BacktestResultRecord>(entity =>
        {
            entity.ToTable("BacktestResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HitRate).HasPrecision(8, 4);
            entity.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<WatchlistRecord>(entity =>
        {
            entity.ToTable("Watchlists");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });
    }
}
