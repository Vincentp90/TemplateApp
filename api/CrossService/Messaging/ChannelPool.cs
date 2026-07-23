using RabbitMQ.Client;

namespace CrossService.Messaging;

/// <summary>
/// Thread-safe channel pool backed by a single long-lived RabbitMQ connection.
/// Creates channels on demand and recycles broken ones automatically.
/// The connection is created lazily on first use, allowing DI test factories
/// to replace the connection factory before the pool is first accessed.
/// </summary>
public class ChannelPool : IDisposable
{
    private readonly Func<Task<IConnection>> _connectionFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public ChannelPool(Func<Task<IConnection>> connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Gets a reusable channel. The channel is long-lived and cached internally.
    /// If the cached channel is broken or null, a new one is created.
    /// The connection is created lazily on first access.
    /// </summary>
    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChannelPool));

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Lazily create the connection on first access (protected by lock)
            if (_connection is null)
            {
                _connection = await _connectionFactory().ConfigureAwait(false);
            }

            if (_channel is null || _channel.IsClosed)
            {
                _channel = await _connection.CreateChannelAsync(null, cancellationToken).ConfigureAwait(false);
            }
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets a fresh channel (not cached). Useful for operations that need their own channel lifecycle.
    /// The connection is created lazily on first access.
    /// </summary>
    public async Task<IChannel> GetFreshChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChannelPool));

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Lazily create the connection on first access (protected by lock)
            if (_connection is null)
            {
                _connection = await _connectionFactory().ConfigureAwait(false);
            }

            return await _connection.CreateChannelAsync(null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Closes the cached channel and resets it.
    /// </summary>
    public async Task ResetChannelAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_channel is not null && !_channel.IsClosed)
            {
                await _channel.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            _channel = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _connection?.Dispose();
        _lock.Dispose();
    }
}
