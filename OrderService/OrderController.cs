using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Models;
using Shared.Events;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderController(IEventStore eventStore, IHttpClientFactory httpClientFactory)
        {
            _eventStore = eventStore;
            _httpClientFactory = httpClientFactory;
        }

        // 1. FE gọi API này để TẠO ĐƠN HÀNG
        [HttpPost]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            var orderId = Guid.NewGuid();
            var orderCreated = new OrderCreatedEvent
            {
                OrderId = orderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                TotalAmount = request.TotalAmount
            };

            // Lưu sự kiện "Đã tạo đơn" vào Event Store
            _eventStore.SaveEvent(orderId, orderCreated);

            // Bắn Webhook gọi InventoryService để trừ kho (Chạy ngầm, không bắt FE chờ)
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(orderCreated), Encoding.UTF8, "application/json");
            _ = client.PostAsync("http://localhost:5002/api/inventory/reserve", content);

            // Trả về ngay lập tức cho FE
            return Ok(new { OrderId = orderId, Message = "Order created, processing in background..." });
        }

        // 2. FE gọi API này để XEM TRẠNG THÁI (Dựa trên Event Sourcing)
        [HttpGet("{orderId}")]
        public IActionResult GetOrder(Guid orderId)
        {
            var events = _eventStore.GetEvents(orderId);
            if (!events.Any()) return NotFound();

            var readModel = new OrderReadModel();
            readModel.Apply(events); // Replay lại toàn bộ lịch sử để lấy trạng thái cuối

            return Ok(readModel);
        }

        // --- CÁC WEBHOOK DÙNG ĐỂ NHẬN PHẢN HỒI TỪ SERVICE KHÁC ---

        [HttpPost("inventory-callback")]
        public IActionResult InventoryCallback([FromBody] InventoryReservedEvent @event)
        {
            _eventStore.SaveEvent(@event.OrderId, @event);

            // Lấy lại giá trị TotalAmount từ EventStore (Ứng dụng thực tế của Event Sourcing)
            var pastEvents = _eventStore.GetEvents(@event.OrderId);
            var orderCreated = pastEvents.OfType<OrderCreatedEvent>().FirstOrDefault();
            decimal amount = orderCreated != null ? orderCreated.TotalAmount : 0;

            // Sau khi kho OK, gọi PaymentService để trừ tiền
            var client = _httpClientFactory.CreateClient();
            var paymentReq = new { OrderId = @event.OrderId, Amount = amount };
            var content = new StringContent(JsonSerializer.Serialize(paymentReq), Encoding.UTF8, "application/json");
            _ = client.PostAsync("http://localhost:5003/api/payment/process", content);

            return Ok();
        }

        [HttpPost("payment-success-callback")]
        public IActionResult PaymentSuccessCallback([FromBody] PaymentProcessedEvent @event)
        {
            _eventStore.SaveEvent(@event.OrderId, @event); // Ghi nhận thành công
            return Ok();
        }

        [HttpPost("payment-failed-callback")]
        public IActionResult PaymentFailedCallback([FromBody] PaymentFailedEvent @event)
        {
            _eventStore.SaveEvent(@event.OrderId, @event); // Ghi nhận thất bại -> Tự động Rollback trạng thái
            return Ok();
        }
    }

    public class OrderRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}