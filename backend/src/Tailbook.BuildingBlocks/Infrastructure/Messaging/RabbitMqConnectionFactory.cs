using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public sealed class RabbitMqConnectionFactory : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.Username,
            Password = _options.Password,
            ContinuationTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(_options.HeartbeatSeconds),
            ClientProvidedName = "tailbook-api",
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}...", _options.Host, _options.Port);
            _connection = await _factory.CreateConnectionAsync(cancellationToken);

            if (_connection is not null)
            {
                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}.", _options.Host, _options.Port);
            }

            return _connection!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}.", _options.Host, _options.Port);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs args)
    {
        _logger.LogWarning("RabbitMQ connection shut down: {Reason}", args.ReplyText);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_connection is not null)
        {
            _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
            await _connection.DisposeAsync();
        }

        _semaphore.Dispose();
    }
}
