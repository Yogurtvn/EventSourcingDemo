namespace Shared.Contracts.Events;

public sealed record OrderCompleted(Guid AggregateId, string Note) : Event(AggregateId);
