using System.Text;
using Majstorio.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Majstorio.WebAPI.Services.Messaging;

// Listens for "notification created" from the Worker and pushes it to that user's phone through the hub.
// It lives in the API because the phones are connected to the API; the notification itself is made by the Worker.
public class NotificationRelay : BackgroundService
{
    private const string CreatedRoutingKey = "notification.created";

    private readonly IConfiguration _configuration;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<NotificationRelay> _logger;

    public NotificationRelay(IConfiguration configuration, IHubContext<NotificationHub> hub,
        ILogger<NotificationRelay> logger)
    {
        _configuration = configuration;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If RabbitMQ is not up yet or the connection drops, log why and connect again.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Notification relay lost RabbitMQ. Trying again in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        var exchange = _configuration["RabbitMq:Exchange"]
            ?? throw new InvalidOperationException("RabbitMq__Exchange is not configured.");

        await using var connection = await RabbitMqPublisher.CreateFactory(_configuration)
            .CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        // A temporary queue with a generated name: a push is only useful while the API is running.
        var queue = (await channel.QueueDeclareAsync(cancellationToken: stoppingToken)).QueueName;
        await channel.QueueBindAsync(queue, exchange, CreatedRoutingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var userId = Encoding.UTF8.GetString(delivery.Body.ToArray());
            await _hub.Clients.User(userId).SendAsync("notificationCreated", stoppingToken);
        };
        // No ack is needed: the notification is already saved, and the phone loads missed ones when it connects.
        await channel.BasicConsumeAsync(queue, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);

        while (connection.IsOpen)
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }
}
