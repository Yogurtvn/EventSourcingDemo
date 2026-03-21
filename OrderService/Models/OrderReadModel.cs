using Shared.Events;

namespace OrderService.Models
{
    public class OrderReadModel
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = "Pending";

        // Hàm Replay để tính toán trạng thái hiện tại từ danh sách Sự kiện
        public void Apply(IEnumerable<object> events)
        {
            foreach (var @event in events)
            {
                switch (@event)
                {
                    case OrderCreatedEvent e:
                        OrderId = e.OrderId;
                        Status = "Created - Chờ Inventory xử lý";
                        break;
                    case InventoryReservedEvent e:
                        Status = "Inventory Reserved - Chờ Payment xử lý";
                        break;
                    case PaymentProcessedEvent e:
                        Status = "Completed - HAPPY CASE: Đơn hàng thành công!";
                        break;
                    case PaymentFailedEvent e:
                        // Đây chính là lúc Rollback được ghi nhận
                        Status = "Cancelled - ROLLBACK CASE: " + e.Reason;
                        break;
                }
            }
        }
    }
}