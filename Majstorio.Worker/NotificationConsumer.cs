using System.Text.Json;
using Majstorio.Model.Messages;
using Majstorio.Services.Database;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Majstorio.Worker;

// Listens on the notifications queue and turns every message into a Notification row.
public sealed class NotificationConsumer : BackgroundService
{
    // Must match the routing key the API publishes with.
    private const string RoutingKey = "notification";
    private const int MaxAttempts = 5;

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
        // If RabbitMQ is not up yet or the connection drops, log why and connect again,
        // so the worker never stops silently.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "RabbitMQ connection failed. Trying again in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        var exchange = Setting("RabbitMq:Exchange");
        var queue = Setting("RabbitMq:Queue");

        // 1. Connect to RabbitMQ with the values from .env.
        var factory = new ConnectionFactory
        {
            HostName = Setting("RabbitMq:Host"),
            Port = int.Parse(Setting("RabbitMq:Port")),
            UserName = Setting("RabbitMq:UserName"),
            Password = Setting("RabbitMq:Password"),
            VirtualHost = Setting("RabbitMq:VirtualHost")
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
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await SaveNotificationAsync(delivery.Body.ToArray());
                    // Tell RabbitMQ the message is done, so it can delete it.
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }
                catch (Exception exception) when (attempt < MaxAttempts)
                {
                    // Retry with exponential backoff: 1 s, 2 s, 4 s, 8 s.
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    _logger.LogWarning(exception, "Attempt {Attempt} failed. Retrying in {Delay}.", attempt, delay);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Message failed {Attempts} times and was dropped.", MaxAttempts);
                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                    return;
                }
            }
        };
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Listening on queue {Queue}.", queue);

        // Stay here while connected; when the connection drops, ExecuteAsync connects again.
        while (connection.IsOpen)
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    // The actual work: one Notification row per message.
    private async Task SaveNotificationAsync(byte[] body)
    {
        var message = JsonSerializer.Deserialize<NotificationMessage>(body)
            ?? throw new InvalidOperationException("The message is empty.");

        // The DbContext is scoped, so each message gets its own.
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();

        // A message delivered again (the worker stopped before its ack) was already saved.
        if (await ctx.Notifications.AnyAsync(notification => notification.EventId == message.EventId))
            return;

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

    private string Setting(string key)
        => _configuration[key] ?? throw new InvalidOperationException($"{key.Replace(":", "__")} is not set in .env.");
}
