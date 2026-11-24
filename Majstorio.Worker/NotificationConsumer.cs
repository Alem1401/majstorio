using System.Text.Json;
using Majstorio.Model.Messages;
using Majstorio.Services.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Majstorio.Worker;

// Listens on the notifications queue and turns every message into a Notification row.
public sealed class NotificationConsumer : BackgroundService
{
    // Must match the routing key the API publishes with.
    private const string RoutingKey = "notification";

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exchange = _configuration["RabbitMq:Exchange"]!;
        var queue = _configuration["RabbitMq:Queue"]!;

        // 1. Connect to RabbitMQ with the values from .env.
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"]!,
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:UserName"]!,
            Password = _configuration["RabbitMq:Password"]!,
            VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/"
        };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // 2. Make sure the exchange and the queue exist, and connect them with the routing key.
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(queue, exchange, RoutingKey, cancellationToken: stoppingToken);

        // 3. Run this code every time a message arrives.
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                await SaveNotificationAsync(delivery.Body.ToArray());
                // Tell RabbitMQ the message is done, so it can delete it.
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception exception)
            {
                // The message is dropped (not requeued) and the reason is logged.
                _logger.LogError(exception, "Could not process message {DeliveryTag}.", delivery.DeliveryTag);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Listening on queue {Queue}.", queue);

        // Keep the worker alive until the app is stopped.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // The actual work: one Notification row per message.
    private async Task SaveNotificationAsync(byte[] body)
    {
        var message = JsonSerializer.Deserialize<NotificationMessage>(body)
            ?? throw new InvalidOperationException("The message is empty.");

        // The DbContext is scoped, so each message gets its own.
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();

        ctx.Notifications.Add(new Notification
        {
            EventId = message.EventId,
            UserId = message.UserId,
            ServiceRequestId = message.ServiceRequestId,
            Title = message.Title,
            Message = message.Message
        });
        await ctx.SaveChangesAsync();
        _logger.LogInformation("Created notification for user {UserId}.", message.UserId);
    }
}
