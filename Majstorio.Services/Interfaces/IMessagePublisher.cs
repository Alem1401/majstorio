using Majstorio.Model.Messages;

// Sends a message to the message broker. Call it after the business change is saved.
public interface IMessagePublisher
{
    Task PublishAsync(NotificationMessage message);
}
