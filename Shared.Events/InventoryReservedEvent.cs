namespace Shared.Events
{
    public record InventoryReservedEvent
    {
        public Guid OrderId { get; init; }
    }
}