using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebFrontend.Pages;

public class AuditTimelineModel(IHttpClientFactory httpClientFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid OrderId { get; set; }

    public List<TimelineItemViewModel> TimelineItems { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        if (OrderId == Guid.Empty)
        {
            ErrorMessage = "Cần truyền OrderId hợp lệ để xem timeline.";
            return;
        }

        var client = httpClientFactory.CreateClient("GatewayClient");

        try
        {
            var orderEventsTask = LoadEventsAsync(client, $"/orders/{OrderId}/events", "Order");
            var inventoryEventsTask = LoadEventsAsync(client, $"/inventory/{OrderId}/events", "Inventory");
            var paymentEventsTask = LoadEventsAsync(client, $"/payments/{OrderId}/events", "Payment");

            await Task.WhenAll(orderEventsTask, inventoryEventsTask, paymentEventsTask);

            TimelineItems = orderEventsTask.Result
                .Concat(inventoryEventsTask.Result)
                .Concat(paymentEventsTask.Result)
                .OrderBy(x => x.Timestamp)
                .ThenBy(x => x.Source)
                .ThenBy(x => x.Version)
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải timeline. Lỗi: {ex.Message}";
        }
    }

    private static async Task<List<TimelineItemViewModel>> LoadEventsAsync(HttpClient client, string path, string source)
    {
        using var response = await client.GetAsync(path);
        if (!response.IsSuccessStatusCode)
        {
            return new List<TimelineItemViewModel>();
        }

        var events = await response.Content.ReadFromJsonAsync<List<EventStoreItemDto>>() ?? new List<EventStoreItemDto>();

        return events.Select(x => new TimelineItemViewModel
        {
            Source = source,
            EventType = x.EventType,
            Timestamp = x.Timestamp,
            Version = x.Version,
            Details = FormatDetails(x.EventData)
        }).ToList();
    }

    private static string FormatDetails(string? eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(eventData);
            var root = document.RootElement;
            var parts = new List<string>();

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("aggregateId") || property.NameEquals("timestamp"))
                {
                    continue;
                }

                if (property.NameEquals("customerId"))
                {
                    parts.Add($"Khách hàng: {property.Value}");
                    continue;
                }

                if (property.NameEquals("totalAmount") || property.NameEquals("amount"))
                {
                    parts.Add($"Số tiền: {property.Value}");
                    continue;
                }

                if (property.NameEquals("reason"))
                {
                    parts.Add($"Lý do: {property.Value}");
                    continue;
                }

                if (property.NameEquals("paymentId"))
                {
                    parts.Add($"Mã thanh toán: {property.Value}");
                    continue;
                }

                if (property.NameEquals("reservedUntil"))
                {
                    parts.Add($"Giữ đến: {property.Value}");
                    continue;
                }

                if (property.NameEquals("testScenario") && property.Value.ValueKind != JsonValueKind.Null)
                {
                    parts.Add($"Kịch bản: {property.Value}");
                }
            }

            return string.Join(" | ", parts);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string GetSourceLabel(string? source) => source switch
    {
        "Order" => "Order Service",
        "Inventory" => "Inventory Service",
        "Payment" => "Payment Service",
        _ => "Unknown Service"
    };

    public static string GetSourceClass(string? source) => source switch
    {
        "Order" => "is-order",
        "Inventory" => "is-inventory",
        "Payment" => "is-payment",
        _ => "is-order"
    };

    public static string GetEventTitle(string? eventType) => eventType switch
    {
        "OrderCreated" => "Đơn hàng được tạo",
        "InventoryReserved" => "Kho đã giữ hàng",
        "InventoryReleased" => "Kho đã hoàn hàng",
        "InventoryFailed" => "Giữ hàng thất bại",
        "PaymentSucceeded" => "Thanh toán thành công",
        "PaymentFailed" => "Thanh toán thất bại",
        "OrderCompleted" => "Đơn hàng hoàn tất",
        "OrderCancelled" => "Đơn hàng bị huỷ",
        _ when string.IsNullOrWhiteSpace(eventType) => "Sự kiện không xác định",
        _ => eventType
    };

    public static string GetEventIcon(string? eventType) => eventType switch
    {
        "OrderCreated" => "bi-plus-circle",
        "InventoryReserved" => "bi-box-seam",
        "InventoryReleased" => "bi-arrow-counterclockwise",
        "InventoryFailed" => "bi-exclamation-diamond",
        "PaymentSucceeded" => "bi-check-circle",
        "PaymentFailed" => "bi-x-circle",
        "OrderCompleted" => "bi-check2-all",
        "OrderCancelled" => "bi-slash-circle",
        _ => "bi-dot"
    };
}

public sealed class EventStoreItemDto
{
    public int Id { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string EventData { get; set; } = string.Empty;
}

public sealed class TimelineItemViewModel
{
    public string Source { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string Details { get; set; } = string.Empty;
}
