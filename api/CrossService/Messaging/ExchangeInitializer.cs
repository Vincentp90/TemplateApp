using RabbitMQ.Client;

namespace CrossService.Messaging;

/// <summary>
/// Configuration for a single exchange declaration.
/// </summary>
public class ExchangeDeclaration
{
    public string ExchangeName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool Durable { get; set; } = true;
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// Configuration for a single queue declaration.
/// </summary>
public class QueueDeclaration
{
    public string QueueName { get; set; } = null!;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// Configuration for a single queue binding.
/// </summary>
public class QueueBinding
{
    public string QueueName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string RoutingKey { get; set; } = "";
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// One-shot initializer for RabbitMQ exchanges, queues, and bindings.
/// Thread-safe: only one initialization runs at a time across all calls.
/// </summary>
public class ExchangeInitializer
{
    private readonly ChannelPool _channelPool;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public ExchangeInitializer(ChannelPool channelPool)
    {
        _channelPool = channelPool ?? throw new ArgumentNullException(nameof(channelPool));
    }

    /// <summary>
    /// Initializes exchanges, queues, and bindings. Safe to call multiple times —
    /// only the first call performs the actual declarations; subsequent calls are no-ops.
    /// </summary>
    public async Task InitializeAsync(
        IEnumerable<ExchangeDeclaration> exchanges,
        IEnumerable<QueueDeclaration> queues,
        IEnumerable<QueueBinding> bindings,
        CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            var channel = await _channelPool.GetChannelAsync(cancellationToken).ConfigureAwait(false);

            // Declare all exchanges
            foreach (var ex in exchanges)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: ex.ExchangeName,
                    type: ex.Type,
                    durable: ex.Durable,
                    arguments: ex.Arguments,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Declare all queues
            foreach (var q in queues)
            {
                await channel.QueueDeclareAsync(
                    queue: q.QueueName,
                    durable: q.Durable,
                    exclusive: q.Exclusive,
                    autoDelete: q.AutoDelete,
                    arguments: q.Arguments,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Bind all queues to exchanges
            foreach (var b in bindings)
            {
                await channel.QueueBindAsync(
                    queue: b.QueueName,
                    exchange: b.ExchangeName,
                    routingKey: b.RoutingKey,
                    arguments: b.Arguments,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
