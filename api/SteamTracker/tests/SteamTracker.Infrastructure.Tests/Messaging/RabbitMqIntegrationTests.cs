using FluentAssertions;
using RabbitMQ.Client;
using SteamTracker.Infrastructure.Tests.TestContainers;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Infrastructure.Tests.Messaging;

/// <summary>
/// Integration tests for RabbitMQ messaging using testcontainers.
/// These tests verify exchange/queue declarations and message publishing/consuming.
/// Skipped when Docker is not available.
/// </summary>
public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private IChannel? _channel;
    private ConnectionFactory? _factory;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        _dockerAvailable = false;
        try
        {
            await RabbitMqContainerFixture.Instance.Container.StartAsync();

            _factory = new ConnectionFactory
            {
                Uri = new Uri(RabbitMqContainerFixture.Instance.Container.GetConnectionString()),
            };

            var connection = await _factory.CreateConnectionAsync();
            _channel = await connection.CreateChannelAsync();
            _dockerAvailable = true;
        }
        catch
        {
            // Docker not available — tests will be skipped
        }
    }

    private void SkipIfNoDocker()
    {
        if (!_dockerAvailable)
        {
            throw new SkipTestException("Docker not available");
        }
    }

    public async Task DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
        await RabbitMqContainerFixture.Instance.Container.StopAsync();
    }

    [Fact]
    public async Task Publish_and_consume_message()
    {
        SkipIfNoDocker();

        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var exchangeName = $"test-exchange-{Guid.NewGuid():N}";

        await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: false);
        await _channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true);
        await _channel.QueueBindAsync(queueName, exchangeName, "test-routing-key");

        var message = new { AppId = 42, Message = "hello" };
        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        // Act
        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: "test-routing-key",
            mandatory: false,
            body: bodyBytes);

        var result = await _channel.BasicGetAsync(queueName, autoAck: true);

        // Assert
        result.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(result!.Body.ToArray());
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.GetProperty("appId").GetInt32().Should().Be(42);
        parsed.GetProperty("message").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task Durable_exchange_and_queue_persist()
    {
        SkipIfNoDocker();

        // Arrange
        var exchangeName = $"durable-exchange-{Guid.NewGuid():N}";
        var queueName = $"durable-queue-{Guid.NewGuid():N}";

        // Act
        await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true);
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: true);

        // Assert — declare again should not throw (idempotent)
        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true);
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: true);
    }

    [Fact]
    public async Task Topic_exchange_with_routing_key()
    {
        SkipIfNoDocker();

        // Arrange
        var exchangeName = $"topic-exchange-{Guid.NewGuid():N}";
        var queueName = $"topic-queue-{Guid.NewGuid():N}";

        await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: false);
        await _channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true);
        await _channel.QueueBindAsync(queueName, exchangeName, "alert.#");

        var message = new { AlertRuleId = Guid.NewGuid(), Price = 15m };
        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        // Act
        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: "alert.triggered",
            mandatory: false,
            body: bodyBytes);

        var result = await _channel.BasicGetAsync(queueName, autoAck: true);

        // Assert
        result.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(result!.Body.ToArray());
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.GetProperty("price").GetDouble().Should().Be(15.0);
    }
}
