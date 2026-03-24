namespace Shared.Contracts.Events;

public abstract record Event
{
    public Guid AggregateId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    protected Event(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }
}
