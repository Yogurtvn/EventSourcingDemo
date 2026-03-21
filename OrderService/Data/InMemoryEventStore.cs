namespace OrderService.Data
{
    public interface IEventStore
    {
        void SaveEvent(Guid streamId, object @event);
        IEnumerable<object> GetEvents(Guid streamId);
    }

    public class InMemoryEventStore : IEventStore
    {
        // Sử dụng Dictionary để lưu trữ events trong bộ nhớ RAM
        // Key là OrderId, Value là danh sách các sự kiện của Order đó
        private readonly Dictionary<Guid, List<object>> _store = new();

        public void SaveEvent(Guid streamId, object @event)
        {
            if (!_store.ContainsKey(streamId))
            {
                _store[streamId] = new List<object>();
            }
            _store[streamId].Add(@event);
        }

        public IEnumerable<object> GetEvents(Guid streamId)
        {
            return _store.TryGetValue(streamId, out var events) ? events : Enumerable.Empty<object>();
        }
    }
}