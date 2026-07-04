using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SteamTracker.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace SteamTracker.Integration.Tests.TestContainers;

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
        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<SteamTrackerDbContext>()
            .UseNpgsql(ConnectionString, o => o.EnableRetryOnFailure())
            .UseInternalServiceProvider(provider)
            .Options;

        var context = new SteamTrackerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
