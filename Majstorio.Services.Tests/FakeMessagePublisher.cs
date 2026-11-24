using Majstorio.Model.Messages;

namespace Majstorio.Services.Tests;

// Stands in for RabbitMQ in tests: remembers what would have been sent.
public class FakeMessagePublisher : IMessagePublisher
{
    public List<NotificationMessage> Sent { get; } = [];

    public Task PublishAsync(NotificationMessage message)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
