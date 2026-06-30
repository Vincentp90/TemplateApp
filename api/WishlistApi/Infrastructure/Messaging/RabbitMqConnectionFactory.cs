using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

/// <summary>
/// Configuration for RabbitMQ connection settings.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Creates RabbitMQ connections using configured host, port, credentials, and virtual host.
/// </summary>
public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly ConnectionFactory _factory;

    public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
    {
        _factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port = options.Value.Port,
            VirtualHost = options.Value.VirtualHost,
            UserName = options.Value.Username,
            Password = options.Value.Password
        };
    }

    public Task<IConnection> CreateConnectionAsync()
    {
        return _factory.CreateConnectionAsync();
    }
}
