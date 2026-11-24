using System.Text;
using System.Text.Json;
using Majstorio.Model.Messages;
using RabbitMQ.Client;

namespace Majstorio.WebAPI.Services.Messaging;


public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    public const string RoutingKey = "notification";

    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(NotificationMessage message)
    {
        
        await _lock.WaitAsync();
        try
        {
            var channel = await GetChannelAsync();
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.EventId.ToString(),
                ContentType = "application/json"
            };
            await channel.BasicPublishAsync(Exchange, RoutingKey, false, properties, body);
        }
        catch (Exception exception)
        {
          
            _logger.LogError(exception, "Could not publish notification {EventId} for user {UserId}.",
                message.EventId, message.UserId);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string Exchange => _configuration["RabbitMq:Exchange"]
        ?? throw new InvalidOperationException("RabbitMq__Exchange is not configured.");

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        
        if (_connection != null)
            await _connection.DisposeAsync();

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMq__Host is not configured."),
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:UserName"] ?? throw new InvalidOperationException("RabbitMq__UserName is not configured."),
            Password = _configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("RabbitMq__Password is not configured."),
            VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/"
        };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}
