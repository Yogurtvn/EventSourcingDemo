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
            ErrorMessage = "OrderId is required.";
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
            ErrorMessage = $"Could not load timeline. Error: {ex.Message}";
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
                    parts.Add($"Customer: {property.Value}");
                    continue;
                }

                if (property.NameEquals("totalAmount") || property.NameEquals("amount"))
                {
                    parts.Add($"Amount: {property.Value}");
                    continue;
                }

                if (property.NameEquals("reason"))
                {
                    parts.Add($"Reason: {property.Value}");
                    continue;
                }

                if (property.NameEquals("paymentId"))
                {
                    parts.Add($"PaymentId: {property.Value}");
                    continue;
                }

                if (property.NameEquals("reservedUntil"))
                {
                    parts.Add($"ReservedUntil: {property.Value}");
                    continue;
                }

                if (property.NameEquals("testScenario") && property.Value.ValueKind != JsonValueKind.Null)
                {
                    parts.Add($"Scenario: {property.Value}");
                }
            }

            return string.Join(" | ", parts);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string GetBorderClass(string? eventType) => eventType switch
    {
        "OrderCreated" => "border-primary",
        "InventoryReserved" => "border-warning",
        "InventoryReleased" => "border-secondary",
        "PaymentSucceeded" => "border-success",
        "PaymentFailed" => "border-danger",
        "OrderCompleted" => "border-success",
        "OrderCancelled" => "border-danger",
        "InventoryFailed" => "border-danger",
        _ => "border-dark"
    };

    public static string GetBadgeClass(string? source) => source switch
    {
        "Order" => "bg-primary",
        "Inventory" => "bg-warning text-dark",
        "Payment" => "bg-success",
        _ => "bg-secondary"
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
