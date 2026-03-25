# Hướng dẫn triển khai NotificationService

`NotificationService` đóng vai trò **Observer**: lắng nghe các sự kiện trên **RabbitMQ** (cùng exchange với các service khác), tra cứu email qua **HTTP OrderService**, gửi mail qua **SendGrid**, có **lưu DB** để audit và **chống gửi trùng** khi message bị retry.

Tài liệu này nằm cùng project `NotificationService`; code chạy thực tế là các file `.cs` trong thư mục này.

---

## Kiến trúc hiện tại (đã có trong repo)

| Thành phần | Vai trò |
|------------|---------|
| `Program.cs` | `AddControllers`, OpenAPI/Swagger (Development), `AddDbContext`, `AddHttpClient`, `AddRabbitMqEventBus`, `AddHostedService<NotificationSagaSubscriber>`, scoped services |
| `Services/NotificationSagaSubscriber.cs` | `IHostedService`: trong `StartAsync` gọi `eventBus.Subscribe<TEvent>(queueName, handler)` cho `OrderCreated`, `OrderCompleted`, `OrderCancelled` |
| `Infrastructure/Http/OrderCatalogClient.cs` | Gọi `GET {OrderService:BaseUrl}/orders/customers` và `GET .../orders/{orderId}` để lấy email |
| `Services/NotificationEmailService.cs` | Gửi mail SendGrid; `SendWithOutcomeAsync` cho ledger |
| `Services/NotificationLedgerService.cs` | Idempotency + ghi `NotificationSendLog` + điều phối gửi mail |
| `Services/EmailTemplateService.cs` | HTML template cho từng loại thông báo |
| `Controllers/NotificationStatusController.cs` | `GET /notifications/health` (và `/api/notifications/health`) |
| `Controllers/NotificationDispatchController.cs` | `GET /notifications/dispatches` — lịch sử gửi |

**Lưu ý quan trọng**

- Interface `IRabbitMqEventBus` **không** có `SubscribeAsync`; chỉ có `Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler)`.
- `ExchangeName` phải trùng các service khác: **`saga.events`**.
- Mỗi loại event dùng **queue riêng**, ví dụ `notification.order-created`, `notification.order-completed`, `notification.order-cancelled`.
- **Không** subscribe `PaymentFailed` trong Notification: khi thanh toán lỗi, `OrderService` publish `OrderCancelled`; Notification chỉ listen `OrderCancelled`.

---

## Bước 1: Khởi tạo project (nếu tạo mới từ đầu)

Chạy từ thư mục gốc solution:

```bash
dotnet new webapi -n NotificationService -f net9.0
dotnet sln add NotificationService/NotificationService.csproj
dotnet add NotificationService/NotificationService.csproj reference Shared.Contracts/Shared.Contracts.csproj
dotnet add NotificationService/NotificationService.csproj reference Shared.Messaging/Shared.Messaging.csproj
dotnet add NotificationService/NotificationService.csproj package SendGrid
dotnet add NotificationService/NotificationService.csproj package Swashbuckle.AspNetCore
```

---

## Bước 2: Cấu hình `appsettings.json`

Điền SendGrid qua **User Secrets** hoặc biến môi trường; **không commit API key** lên Git.

```json
{
  "OrderService": {
    "BaseUrl": "http://localhost:5044"
  },
  "ConnectionStrings": {
    "NotificationDb": "Data Source=notification-service.db",
    "NotificationDbPostgres": "Host=localhost;Port=5432;Database=notification_service;Username=postgres;Password=***"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "saga.events",
    "StartupConnectRetryCount": 30,
    "StartupConnectRetryDelayMs": 1000,
    "RetryCount": 3,
    "RetryDelayMs": 200
  },
  "SendGrid": {
    "ApiKey": "",
    "FromEmail": "",
    "FromName": "EventSourcingDemo"
  }
}
```

**User Secrets (khuyên dùng khi dev):**

```bash
cd NotificationService
dotnet user-secrets init
dotnet user-secrets set "SendGrid:ApiKey" "SG...."
dotnet user-secrets set "SendGrid:FromEmail" "verified-sender@yourdomain.com"
```

---

## Bước 3: Subscribe RabbitMQ

Trong `NotificationSagaSubscriber.StartAsync`, dùng `eventBus.Subscribe<...>(queueName, handler)` và `IServiceScopeFactory.CreateScope()` trong handler.

---

## Bước 4: `Program.cs`

- `AddControllers`, OpenAPI/Swagger (Development)
- `AddDbContext<NotificationDbContext>` (Postgres nếu `NotificationDbPostgres` khác rỗng, không thì SQLite)
- `EnsureCreated()` khi khởi động
- `AddHttpClient`, scoped services, `AddRabbitMqEventBus`, `AddHostedService<NotificationSagaSubscriber>`

Cổng trong `Properties/launchSettings.json` nên **khác** OrderService (ví dụ `5180`).

---

## Bước 5: Lưu DB — log + idempotency

Đã triển khai trong project này:

| File | Nội dung |
|------|----------|
| `Models/NotificationSendLog.cs` | Entity log + idempotency key |
| `Infrastructure/Persistence/NotificationDbContext.cs` | EF Core, bảng `NotificationSendLog`, unique `IdempotencyKey` |
| `Services/NotificationLedgerService.cs` | Kiểm tra key, gửi mail, ghi log; bắt unique violation khi race |
| `Services/EmailSendOutcome.cs` | Delivered / SkippedNotConfigured / ProviderRejected |
| `Services/NotificationDispatchStatus.cs` | Hằng số trạng thái log |

Database `notification_service` trên Postgres: xem `docs/sql/init-postgres.sql` ở thư mục gốc solution.

**API lịch sử:** `GET /notifications/dispatches` hoặc `GET /api/notifications/dispatches?take=50` (`take` tối đa 500).

---

## Tóm tắt luồng dữ liệu

1. Client tạo đơn → `OrderService` publish `OrderCreated`.
2. Notification nhận event → HTTP lấy email → SendGrid (HTML) → ghi log.
3. Saga thành công → `OrderCompleted` → mail hoàn tất + log.
4. Hủy đơn → `OrderCancelled` → mail hủy + log.

---

## Checklist

- [x] EF + DbContext + `EnsureCreated()`
- [x] `NotificationLedgerService` + `NotificationSagaSubscriber`
- [ ] Không commit secret SendGrid; dùng User Secrets / env trên CI
