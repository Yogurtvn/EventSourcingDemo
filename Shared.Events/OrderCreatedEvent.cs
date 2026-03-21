namespace Shared.Events
{
    public record OrderCreatedEvent
    {
        public Guid OrderId { get; init; }
        public string ProductId { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal TotalAmount { get; init; }
    }
}