using System.Net.Http.Json;

namespace NotificationService.Infrastructure.Http;

public sealed class OrderCatalogClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OrderCatalogClient> logger)
{
    private string BaseUrl => (configuration["OrderService:BaseUrl"] ?? "http://localhost:5044").TrimEnd('/');

    public async Task<string?> GetEmailByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var customers = await client.GetFromJsonAsync<List<CustomerDto>>(
                $"{BaseUrl}/orders/customers",
                cancellationToken);
            return customers?.FirstOrDefault(c => c.CustomerId == customerId)?.Email;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch customers from OrderService");
            return null;
        }
    }

    public async Task<string?> GetEmailByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var order = await client.GetFromJsonAsync<OrderDto>(
                $"{BaseUrl}/orders/{orderId}",
                cancellationToken);
            if (order is null)
            {
                return null;
            }

            return await GetEmailByCustomerIdAsync(order.CustomerId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch order {OrderId} from OrderService", orderId);
            return null;
        }
    }

    private sealed class CustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    private sealed class OrderDto
    {
        public string CustomerId { get; set; } = string.Empty;
    }
}
