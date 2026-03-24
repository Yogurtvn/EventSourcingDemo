namespace OrderService.Events;

public sealed record OrderCreated(Guid AggregateId, string CustomerId, decimal TotalAmount) : Event(AggregateId);
