using Microsoft.EntityFrameworkCore;
using SteamTracker.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace SteamTracker.Infrastructure.Tests.TestContainers;

/// <summary>
/// Exception used to signal that tests should be skipped.
/// </summary>
public class SkipTestException : Exception
{
    public SkipTestException(string message) : base(message) { }
}

/// <summary>
/// Shared fixture for a Postgres container. Created once per test run.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private static readonly Lazy<PostgresContainerFixture> _instance = new(() => new());

    public static PostgresContainerFixture Instance => _instance.Value;

    public PostgreSqlContainer Container { get; }
    public string ConnectionString => Container.GetConnectionString();

    private PostgresContainerFixture()
    {
        Container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithPassword("testpassword")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }

    public SteamTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SteamTrackerDbContext>()
            .UseNpgsql(ConnectionString, o => o.EnableRetryOnFailure())
            .Options;

        var context = new SteamTrackerDbContext(options);
        context.Database.Migrate();
        return context;
    }
}
