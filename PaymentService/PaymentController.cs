using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using System.Text.Json;
using System.Text;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            await Task.Delay(1000); // Giả lập gọi API ngân hàng

            var client = _httpClientFactory.CreateClient();

            // MÔ PHỎNG LỖI (ROLLBACK CASE): Nếu mua đơn trên 1 triệu
            if (request.Amount > 1000000)
            {
                var failedEvent = new PaymentFailedEvent
                {
                    OrderId = request.OrderId,
                    Reason = "Thẻ không đủ số dư để thanh toán đơn hàng lớn."
                };
                var content = new StringContent(JsonSerializer.Serialize(failedEvent), Encoding.UTF8, "application/json");
                await client.PostAsync("http://localhost:5001/api/order/payment-failed-callback", content);
            }
            // MÔ PHỎNG THÀNH CÔNG (HAPPY CASE)
            else
            {
                var successEvent = new PaymentProcessedEvent { OrderId = request.OrderId };
                var content = new StringContent(JsonSerializer.Serialize(successEvent), Encoding.UTF8, "application/json");
                await client.PostAsync("http://localhost:5001/api/order/payment-success-callback", content);
            }

            return Ok();
        }
    }

    public class PaymentRequest
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}