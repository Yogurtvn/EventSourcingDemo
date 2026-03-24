namespace OrderService.Events;

public sealed record OrderCompleted(Guid AggregateId, string Note) : Event(AggregateId);
