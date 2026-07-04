using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace SteamTracker.Integration.Tests.TestContainers;

/// <summary>
/// Shared fixture for a RabbitMQ container. Created once per test run.
/// </summary>
public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private static readonly Lazy<RabbitMqContainerFixture> _instance = new(() => new());

    public static RabbitMqContainerFixture Instance => _instance.Value;

    public RabbitMqContainer Container { get; }
    public string ConnectionString => Container.GetConnectionString();

    private RabbitMqContainerFixture()
    {
        Container = new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        // Wait for RabbitMQ to be ready
        var factory = new ConnectionFactory
        {
            Uri = new Uri(Container.GetConnectionString()),
        };
        await using var conn = await factory.CreateConnectionAsync();
        await using var channel = await conn.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(string.Empty, ExchangeType.Direct, false, true);
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }
}
