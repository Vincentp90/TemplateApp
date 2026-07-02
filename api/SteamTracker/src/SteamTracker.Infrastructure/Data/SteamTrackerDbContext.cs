using Microsoft.EntityFrameworkCore;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data.Config;

namespace SteamTracker.Infrastructure.Data;

public class SteamTrackerDbContext : DbContext
{
    public SteamTrackerDbContext(DbContextOptions<SteamTrackerDbContext> options)
        : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<TrackedGame> TrackedGames => Set<TrackedGame>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SteamTrackerDbContext).Assembly);

        // Convert SteamAppId value object to int for EF Core
        modelBuilder.Entity<Game>().Property(g => g.AppId)
            .HasConversion(v => v.Value, v => new SteamAppId(v));
        modelBuilder.Entity<TrackedGame>().Property(tg => tg.AppId)
            .HasConversion(v => v.Value, v => new SteamAppId(v));
        modelBuilder.Entity<PriceSnapshot>().Property(ps => ps.GameId)
            .HasConversion(v => v.Value, v => new SteamAppId(v));

        // Indexes
        modelBuilder.Entity<TrackedGame>()
            .HasIndex(tg => tg.AppId);

        modelBuilder.Entity<AlertRule>()
            .HasIndex(ar => new { ar.UserId, ar.AppId });

        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(ps => new { ps.GameId, ps.CapturedAt });
    }
}
