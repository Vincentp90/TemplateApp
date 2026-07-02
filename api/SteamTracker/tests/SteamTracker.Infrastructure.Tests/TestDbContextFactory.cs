using Microsoft.EntityFrameworkCore;
using SteamTracker.Infrastructure.Data;

namespace SteamTracker.Infrastructure.Tests;

/// <summary>
/// Creates an in-memory DbContext for integration tests.
/// </summary>
public static class TestDbContextFactory
{
    public static SteamTrackerDbContext Create()
    {
        var options = new DbContextOptionsBuilder<SteamTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"SteamTracker_{Guid.NewGuid():N}")
            .Options;

        var context = new SteamTrackerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void Dispose(SteamTrackerDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
