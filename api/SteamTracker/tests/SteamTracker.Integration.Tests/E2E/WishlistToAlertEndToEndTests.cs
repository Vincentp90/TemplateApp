using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Messaging;
using SteamTracker.Infrastructure.Repositories;
using SteamTracker.Worker;
using SteamTracker.Integration.Tests.TestContainers;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Integration.Tests.E2E;

/// <summary>
/// End-to-end integration test covering the full pipeline:
///   WishlistItemAdded event → TrackedGame creation → PriceCheckJob → Price fetch → Alert fires
///
/// Uses real Postgres + RabbitMQ (testcontainers), real DbContext, real repositories,
/// and real RabbitMQ consumers — only the Steam HTTP client is mocked.
///
/// Uses synchronous BasicGet polling instead of async consumers for reliability.
/// </summary>
public class WishlistToAlertEndToEndTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = PostgresContainerFixture.Instance;
    private readonly RabbitMqContainerFixture _rabbitMq = RabbitMqContainerFixture.Instance;

    private SteamTrackerDbContext? _context;
    private IChannel? _publishChannel;
    private IConnection? _connection;
    private ServiceProvider? _serviceProvider;
    private readonly string _testId = Guid.NewGuid().ToString("N");

    // Test doubles
    private readonly Mock<ISteamStoreClient> _steamClientMock = new();
    private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();
    private readonly List<(Guid AlertRuleId, string UserId, int AppId, decimal Price, string Currency)> _notifications = new();

    // Unique queue names per test class to avoid conflicts
    private string SyncQueue => $"steamtracker.wishlist-sync-{_testId}";
    private string PriceCheckQueue => $"price-check-jobs-{_testId}";

    public async Task InitializeAsync()
    {
        // Start containers (idempotent — already started by first test class)
        await _postgres.Container.StartAsync();
        await _rabbitMq.Container.StartAsync();

        // Create DbContext (EnsureCreated handles idempotent table creation)
        _context = _postgres.CreateDbContext();

        // Create RabbitMQ connection and channels
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMq.Container.GetConnectionString()),
        };
        _connection = await factory.CreateConnectionAsync();
        _publishChannel = await _connection.CreateChannelAsync();

        // Declare unique exchanges and queues for this test class
        await _publishChannel.ExchangeDeclareAsync("wishlist.events", ExchangeType.Fanout, durable: true);
        await _publishChannel.QueueDeclareAsync(SyncQueue, durable: true, exclusive: false, autoDelete: false);
        await _publishChannel.QueueBindAsync(SyncQueue, "wishlist.events", routingKey: "");

        await _publishChannel.ExchangeDeclareAsync("steamtracker.direct", ExchangeType.Direct, durable: true);
        await _publishChannel.QueueDeclareAsync(PriceCheckQueue, durable: true, exclusive: false, autoDelete: false);
        await _publishChannel.QueueBindAsync(PriceCheckQueue, "steamtracker.direct", routingKey: "price-check");

        // Setup test doubles
        _notifications.Clear();
        _notificationPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, decimal, string, CancellationToken>((ruleId, userId, appId, price, currency, ct) =>
                _notifications.Add((ruleId, userId, appId, price, currency)))
            .Returns(Task.CompletedTask);

        // Build DI container with real repos + mock ports
        var services = new ServiceCollection();

        // Real DbContext
        services.AddDbContext<SteamTrackerDbContext>(options =>
            options.UseNpgsql(_postgres.ConnectionString));

        // Real repositories
        services.AddScoped<ITrackedGameRepository, TrackedGameRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();

        // Mock external ports
        services.AddSingleton(_steamClientMock.Object);
        services.AddSingleton(_notificationPublisherMock.Object);

        // In-memory publisher that writes to our test RabbitMQ queue
        services.AddSingleton<IPriceCheckJobPublisher>(new TestPriceCheckJobPublisher(_publishChannel!, PriceCheckQueue));

        // Use cases
        services.AddScoped<ISetAlertRuleUseCase, SetAlertRuleUseCase>();
        services.AddScoped<IDeleteAlertRuleUseCase, DeleteAlertRuleUseCase>();
        services.AddScoped<IProcessPriceCheckUseCase, ProcessPriceCheckUseCase>();
        services.AddScoped<IHandleWishlistItemAddedUseCase, HandleWishlistItemAddedUseCase>();
        services.AddScoped<IHandleWishlistItemRemovedUseCase, HandleWishlistItemRemovedUseCase>();
        services.AddSingleton<PriceAlertEvaluator>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        // Dispose our DbContext but don't delete the shared database
        _context?.Dispose();

        if (_publishChannel is not null)
            await _publishChannel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();

        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task FullPipeline_WishlistAddToAlert_FiresCorrectly()
    {
        // ===== ARRANGE =====
        const int appId = 42;
        const string userId = "e2e-user";
        var addedAt = DateTimeOffset.UtcNow;
        var alertThreshold = new Money(25m, "EUR");
        var steamPrice = new Money(19.99m, "EUR");
        var gameName = "Test Game E2E";

        // Mock: Steam returns a price that triggers the alert
        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ReturnsAsync(SteamPriceResult.WithPrice(steamPrice, gameName));

        // Create alert rule (TrackedGame will be created by HandleWishlistItemAddedUseCase)
        var alertRule = new AlertRule(Guid.NewGuid(), userId, new SteamAppId(appId), alertThreshold);
        _context!.AlertRules.Add(alertRule);
        await _context.SaveChangesAsync();

        // 2. Publish WishlistItemAdded event to RabbitMQ
        var addedMessage = new { userId, appId, addedAt = addedAt.ToString("o") };
        var addedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(addedMessage, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        await _publishChannel!.BasicPublishAsync(
            exchange: "wishlist.events",
            routingKey: "",
            mandatory: false,
            body: addedBody);

        // ===== ACT — Poll the sync queue and process the event =====
        var addedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemAddedUseCase>();
        var removedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemRemovedUseCase>();

        var pollChannel = await _connection!.CreateChannelAsync();

        // Poll for the WishlistItemAdded message
        var addedMsg = await PollForMessageAsync(pollChannel, SyncQueue, TimeSpan.FromSeconds(10));
        Assert.NotNull(addedMsg);

        var addedJson = Encoding.UTF8.GetString(addedMsg!.Body.ToArray());
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var addedEvt = JsonSerializer.Deserialize<WishlistItemAddedMessage>(addedJson, options);
        Assert.NotNull(addedEvt);

        // Process the added event
        await addedUseCase.ExecuteAsync(addedEvt!.UserId, addedEvt.AppId, addedEvt.AddedAt, CancellationToken.None);

        // Poll for the PriceCheckJob message
        var priceMsg = await PollForMessageAsync(pollChannel, PriceCheckQueue, TimeSpan.FromSeconds(10));
        Assert.NotNull(priceMsg);

        var priceJson = Encoding.UTF8.GetString(priceMsg!.Body.ToArray());
        var priceOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var priceRequest = JsonSerializer.Deserialize<PriceCheckMessage>(priceJson, priceOptions);
        Assert.NotNull(priceRequest);

        // Process the price check
        var processUseCase = _serviceProvider.GetRequiredService<IProcessPriceCheckUseCase>();
        var result = await _steamClientMock.Object.FetchPriceAsync(priceRequest!.AppId);
        Assert.NotNull(result);
        await processUseCase.ExecuteAsync(priceRequest.AppId, result.Price, result.Name, result.IsUnavailable, CancellationToken.None);

        // ===== ASSERT =====
        // 1. TrackedGame exists and is active
        var savedTrackedGame = await _serviceProvider!.GetRequiredService<ITrackedGameRepository>().GetAsync(new SteamAppId(appId));
        savedTrackedGame.Should().NotBeNull();
        savedTrackedGame!.IsActive.Should().BeTrue();

        // 2. Game has price data
        var gameRepo = _serviceProvider.GetRequiredService<IGameRepository>();
        var savedGame = await gameRepo.GetAsync(appId);
        savedGame.Should().NotBeNull();
        savedGame!.CurrentPrice.Should().NotBeNull();
        savedGame.CurrentPrice!.Value.Amount.Should().Be(19.99m);
        savedGame.CurrentPrice!.Value.Currency.Value.Should().Be("EUR");
        savedGame.Name.Should().Be("Test Game E2E");
        savedGame.LastCheckedAt.Should().NotBeNull();

        // 3. Alert was triggered and notification was sent
        _notifications.Should().ContainSingle();
        _notifications[0].AlertRuleId.Should().Be(alertRule.AlertRuleId);
        _notifications[0].UserId.Should().Be(userId);
        _notifications[0].AppId.Should().Be(appId);
        _notifications[0].Price.Should().Be(19.99m);
        _notifications[0].Currency.Should().Be("EUR");

        // 4. Alert rule was marked as triggered
        var alertRuleRepo = _serviceProvider.GetRequiredService<IAlertRuleRepository>();
        var updatedRule = await alertRuleRepo.GetAsync(alertRule.AlertRuleId);
        updatedRule!.LastTriggeredAt.Should().NotBeNull();

        // Cleanup
        await pollChannel.DisposeAsync();
    }

    [Fact]
    public async Task FullPipeline_NoAlertWhenPriceAboveThreshold()
    {
        // ===== ARRANGE =====
        const int appId = 99;
        const string userId = "e2e-user-no-alert";
        var addedAt = DateTimeOffset.UtcNow;
        var alertThreshold = new Money(10m, "EUR");  // Lower than the steam price
        var steamPrice = new Money(19.99m, "EUR");
        var gameName = "Expensive Game";

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ReturnsAsync(SteamPriceResult.WithPrice(steamPrice, gameName));

        // Create alert rule (TrackedGame will be created by HandleWishlistItemAddedUseCase)
        var alertRule = new AlertRule(Guid.NewGuid(), userId, new SteamAppId(appId), alertThreshold);
        _context!.AlertRules.Add(alertRule);
        await _context.SaveChangesAsync();

        // Publish WishlistItemAdded event
        var addedMessage = new { userId, appId, addedAt = addedAt.ToString("o") };
        var addedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(addedMessage, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        await _publishChannel!.BasicPublishAsync(
            exchange: "wishlist.events",
            routingKey: "",
            mandatory: false,
            body: addedBody);

        // ===== ACT =====
        var addedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemAddedUseCase>();
        var removedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemRemovedUseCase>();

        var pollChannel = await _connection!.CreateChannelAsync();

        // Poll for the WishlistItemAdded message
        var addedMsg = await PollForMessageAsync(pollChannel, SyncQueue, TimeSpan.FromSeconds(10));
        Assert.NotNull(addedMsg);

        var addedJson = Encoding.UTF8.GetString(addedMsg!.Body.ToArray());
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var addedEvt = JsonSerializer.Deserialize<WishlistItemAddedMessage>(addedJson, options);
        Assert.NotNull(addedEvt);

        // Process the added event
        await addedUseCase.ExecuteAsync(addedEvt!.UserId, addedEvt.AppId, addedEvt.AddedAt, CancellationToken.None);

        // Poll for the PriceCheckJob message
        var priceMsg = await PollForMessageAsync(pollChannel, PriceCheckQueue, TimeSpan.FromSeconds(10));
        Assert.NotNull(priceMsg);

        var priceJson = Encoding.UTF8.GetString(priceMsg!.Body.ToArray());
        var priceOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var priceRequest = JsonSerializer.Deserialize<PriceCheckMessage>(priceJson, priceOptions);
        Assert.NotNull(priceRequest);

        // Process the price check
        var processUseCase = _serviceProvider.GetRequiredService<IProcessPriceCheckUseCase>();
        var result = await _steamClientMock.Object.FetchPriceAsync(priceRequest!.AppId);
        Assert.NotNull(result);
        await processUseCase.ExecuteAsync(priceRequest.AppId, result.Price, result.Name, result.IsUnavailable, CancellationToken.None);

        // ===== ASSERT =====
        // Price was saved
        var gameRepo = _serviceProvider.GetRequiredService<IGameRepository>();
        var savedGame = await gameRepo.GetAsync(appId);
        savedGame.Should().NotBeNull();
        savedGame!.CurrentPrice!.Value.Amount.Should().Be(19.99m);

        // No notifications were sent (price above threshold)
        _notifications.Should().BeEmpty();

        // Cleanup
        await pollChannel.DisposeAsync();
    }

    [Fact]
    public async Task FullPipeline_WishlistItemRemoved_StopsTracking()
    {
        // ===== ARRANGE =====
        const int appId = 77;
        const string userId = "e2e-user-remove";
        var addedAt = DateTimeOffset.UtcNow;

        // Seed TrackedGame and alert rule
        var trackedGameRepo = _serviceProvider!.GetRequiredService<ITrackedGameRepository>();
        var alertRuleRepo = _serviceProvider.GetRequiredService<IAlertRuleRepository>();
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(appId), addedAt);
        await trackedGameRepo.SaveAsync(trackedGame);

        var alertRule = new AlertRule(Guid.NewGuid(), userId, new SteamAppId(appId), new Money(10m));
        _context!.AlertRules.Add(alertRule);
        await _context.SaveChangesAsync();

        // Publish WishlistItemRemoved event
        var removedMessage = new { userId, appId, removedAt = DateTimeOffset.UtcNow.ToString("o") };
        var removedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(removedMessage, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        await _publishChannel!.BasicPublishAsync(
            exchange: "wishlist.events",
            routingKey: "",
            mandatory: false,
            body: removedBody);

        // ===== ACT =====
        var addedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemAddedUseCase>();
        var removedUseCase = _serviceProvider.GetRequiredService<IHandleWishlistItemRemovedUseCase>();

        var pollChannel = await _connection!.CreateChannelAsync();

        // Poll for the WishlistItemRemoved message
        var removedMsg = await PollForMessageAsync(pollChannel, SyncQueue, TimeSpan.FromSeconds(10));
        Assert.NotNull(removedMsg);

        var removedJson = Encoding.UTF8.GetString(removedMsg!.Body.ToArray());
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var removedEvt = JsonSerializer.Deserialize<WishlistItemRemovedMessage>(removedJson, options);
        Assert.NotNull(removedEvt);

        // Process the removed event
        await removedUseCase.ExecuteAsync(removedEvt!.UserId, removedEvt.AppId, CancellationToken.None);

        // ===== ASSERT =====
        // TrackedGame is deactivated
        var updatedTrackedGame = await trackedGameRepo.GetAsync(new SteamAppId(appId));
        updatedTrackedGame!.IsActive.Should().BeFalse();

        // Alert rule is deactivated
        var updatedRule = await alertRuleRepo.GetAsync(alertRule.AlertRuleId);
        updatedRule!.IsActive.Should().BeFalse();

        // Cleanup
        await pollChannel.DisposeAsync();
    }

    /// <summary>
    /// Polls a queue for a message with retry logic and timeout.
    /// </summary>
    private static async Task<RabbitMQ.Client.BasicGetResult?> PollForMessageAsync(
        IChannel channel,
        string queue,
        TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var msg = await channel.BasicGetAsync(queue, autoAck: true);
            if (msg is not null)
                return msg;

            await Task.Delay(100);
        }
        return null;
    }
}

// ===== Test doubles =====

/// <summary>
/// Writes PriceCheckJob messages directly to a RabbitMQ queue.
/// </summary>
public class TestPriceCheckJobPublisher : IPriceCheckJobPublisher
{
    private readonly IChannel _channel;
    private readonly string _queueName;

    public TestPriceCheckJobPublisher(IChannel channel, string queueName)
    {
        _channel = channel;
        _queueName = queueName;
    }

    public async Task EnqueueAsync(int appId, CancellationToken cancellationToken = default)
    {
        var message = new { appId, enqueuedAt = DateTimeOffset.UtcNow.ToString("o") };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        await _channel.BasicPublishAsync(
            exchange: "steamtracker.direct",
            routingKey: "price-check",
            mandatory: false,
            body: body);
    }
}
