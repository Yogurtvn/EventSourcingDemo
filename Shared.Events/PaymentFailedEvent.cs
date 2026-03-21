namespace Shared.Events
{
    public record PaymentFailedEvent
    {
        public Guid OrderId { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}