using RabbitMQ.Client;

namespace CrossService.Messaging;

/// <summary>
/// Thread-safe channel pool backed by a single long-lived RabbitMQ connection.
/// Creates channels on demand and recycles broken ones automatically.
/// </summary>
public class ChannelPool : IDisposable
{
    private readonly IConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;
    private bool _disposed;

    public ChannelPool(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Gets a reusable channel. The channel is long-lived and cached internally.
    /// If the cached channel is broken or null, a new one is created.
    /// </summary>
    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChannelPool));

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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
    /// </summary>
    public async Task<IChannel> GetFreshChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChannelPool));

        return await _connection.CreateChannelAsync(null, cancellationToken).ConfigureAwait(false);
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
        _lock.Dispose();
    }
}
