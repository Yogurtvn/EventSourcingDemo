namespace Shared.Contracts.Events;

public static class EventTypeMap
{
    private static readonly Dictionary<string, Type> TypeByName = new(StringComparer.Ordinal)
    {
        [nameof(OrderCreated)] = typeof(OrderCreated),
        [nameof(InventoryReserved)] = typeof(InventoryReserved),
        [nameof(InventoryFailed)] = typeof(InventoryFailed),
        [nameof(PaymentSucceeded)] = typeof(PaymentSucceeded),
        [nameof(PaymentFailed)] = typeof(PaymentFailed),
        [nameof(InventoryReleased)] = typeof(InventoryReleased),
        [nameof(OrderCompleted)] = typeof(OrderCompleted),
        [nameof(OrderCancelled)] = typeof(OrderCancelled)
    };

    public static Type? Resolve(string eventType)
    {
        return TypeByName.GetValueOrDefault(eventType);
    }
}
