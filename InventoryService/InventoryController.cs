using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using System.Text.Json;
using System.Text;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InventoryController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("reserve")]
        public async Task<IActionResult> ReserveInventory([FromBody] OrderCreatedEvent @event)
        {
            await Task.Delay(1000); // Giả lập thời gian database trừ kho

            var reservedEvent = new InventoryReservedEvent { OrderId = @event.OrderId };

            // Trừ kho thành công, báo lại cho Webhook của OrderService
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(reservedEvent), Encoding.UTF8, "application/json");
            await client.PostAsync("http://localhost:5001/api/order/inventory-callback", content);

            return Ok();
        }
    }
}