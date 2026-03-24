using OrderService.Events;

namespace OrderService.Infrastructure.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : Event;

    void Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : Event;
}
