using Shared.Contracts.Events;

namespace Shared.Messaging.RabbitMq;

public interface IRabbitMqEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : Event;

    void Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler)
        where TEvent : Event;
}
