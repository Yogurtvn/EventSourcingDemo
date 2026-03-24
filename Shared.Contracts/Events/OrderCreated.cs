namespace Shared.Contracts.Events;

public sealed record OrderCreated(Guid AggregateId, string CustomerId, decimal TotalAmount, string? TestScenario = null) : Event(AggregateId);
