using OrderService.Events;

namespace OrderService.Infrastructure.EventBus;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<Event, Task>>> _handlers = new();
    private readonly object _sync = new();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : Event
    {
        List<Func<Event, Task>> handlers;
        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var subscriptions))
            {
                return Task.CompletedTask;
            }

            handlers = subscriptions.ToList();
        }

        return ExecuteHandlersAsync(handlers, @event, cancellationToken);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : Event
    {
        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var subscriptions))
            {
                subscriptions = new List<Func<Event, Task>>();
                _handlers[typeof(TEvent)] = subscriptions;
            }

            subscriptions.Add(@event => handler((TEvent)@event));
        }
    }

    private static async Task ExecuteHandlersAsync(
        IEnumerable<Func<Event, Task>> handlers,
        Event @event,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(@event);
        }
    }
}
