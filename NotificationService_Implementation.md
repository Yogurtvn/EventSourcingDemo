# Hướng dẫn triển khai NotificationService

`NotificationService` đóng vai trò **Observer**: lắng nghe các sự kiện trên **RabbitMQ** (cùng exchange với các service khác), tra cứu email qua **HTTP OrderService**, gửi mail qua **SendGrid**, có thể bổ sung **lưu DB** để audit và **chống gửi trùng** khi message bị retry.

Trong repo hiện tại, phần chạy thực tế nằm tại thư mục `NotificationService/` (đã có sẵn). Tài liệu này mô tả kiến trúc đúng với code và hướng dẫn mở rộng (DB + idempotency).

---

## Kiến trúc hiện tại (đã có trong repo)

| Thành phần | Vai trò |
|------------|---------|
| `Program.cs` | `AddControllers`, OpenAPI/Swagger (Development), `AddHttpClient`, `AddRabbitMqEventBus`, `AddHostedService<NotificationSagaSubscriber>`, scoped services |
| `Services/NotificationSagaSubscriber.cs` | `IHostedService`: trong `StartAsync` gọi `eventBus.Subscribe<TEvent>(queueName, handler)` cho `OrderCreated`, `OrderCompleted`, `OrderCancelled` |
| `Infrastructure/Http/OrderCatalogClient.cs` | Gọi `GET {OrderService:BaseUrl}/orders/customers` và `GET .../orders/{orderId}` để lấy email |
| `Services/NotificationEmailService.cs` | Gửi mail SendGrid; nếu thiếu `ApiKey`/`FromEmail` thì chỉ log cảnh báo (không throw) |
| `Services/EmailTemplateService.cs` | HTML template cho từng loại thông báo |
| `Controllers/NotificationStatusController.cs` | `GET /notifications/health` (và `/api/notifications/health`) |

**Lưu ý quan trọng**

- Interface `IRabbitMqEventBus` **không** có `SubscribeAsync`; chỉ có `Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler)`.
- `ExchangeName` phải trùng các service khác: **`saga.events`** (không dùng `eventsourcing_exchange` nếu muốn nhận được event từ Order/Inventory/Payment).
- Mỗi loại event dùng **queue riêng**, ví dụ `notification.order-created`, `notification.order-completed`, `notification.order-cancelled` (RabbitMQ bind queue → routing key = tên class event, ví dụ `OrderCreated`).
- **Không** subscribe `PaymentFailed` trong Notification: khi thanh toán lỗi, `OrderService` xử lý và publish `OrderCancelled`; Notification chỉ cần listen `OrderCancelled` để mail hủy đơn.

---

## Bước 1: Khởi tạo project (nếu tạo mới từ đầu)

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

Ví dụ tối thiểu (điền SendGrid qua **User Secrets** hoặc biến môi trường trên máy dev; **không commit API key** lên Git):

```json
{
  "OrderService": {
    "BaseUrl": "http://localhost:5044"
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

## Bước 3: Subscribe RabbitMQ (đúng API thực tế)

Trong `NotificationSagaSubscriber.StartAsync`:

```csharp
eventBus.Subscribe<OrderCreated>("notification.order-created", async @event =>
{
    using var scope = scopeFactory.CreateScope();
    // resolve OrderCatalogClient, NotificationEmailService, EmailTemplateService...
});

eventBus.Subscribe<OrderCompleted>("notification.order-completed", async @event => { ... });
eventBus.Subscribe<OrderCancelled>("notification.order-cancelled", async @event => { ... });
```

Handler nên dùng `IServiceScopeFactory` + `CreateScope()` vì `Subscribe` chạy ngoài scope HTTP.

---

## Bước 4: `Program.cs` (giống pattern Order/Inventory/Payment)

- `AddControllers`, `AddOpenApi`, `AddEndpointsApiExplorer`, `AddSwaggerGen`
- Development: `MapOpenApi`, `UseSwagger`, `UseSwaggerUI`
- `AddHttpClient`, đăng ký scoped services, `AddRabbitMqEventBus`, `AddHostedService<NotificationSagaSubscriber>`
- `MapControllers()`

Cổng chạy mặc định trong `Properties/launchSettings.json` của Notification nên **khác** OrderService (ví dụ `5180`) để tránh trùng `5044`.

---

## Bước 5 (mở rộng): Lưu DB — log đã gửi + idempotency (chống gửi trùng khi Rabbit retry)

`Shared.Messaging` khi handler **throw** có thể **republish** message → consumer có thể xử lý lại cùng một event → dễ gửi mail trùng nếu không có idempotency.

### 5.1. Gói NuGet

```bash
dotnet add NotificationService/NotificationService.csproj package Microsoft.EntityFrameworkCore --version 9.0.9
dotnet add NotificationService/NotificationService.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.9
dotnet add NotificationService/NotificationService.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4
```

### 5.2. Connection strings (cùng pattern các service khác)

```json
"ConnectionStrings": {
  "NotificationDb": "Data Source=notification-service.db",
  "NotificationDbPostgres": "Host=localhost;Port=5432;Database=notification_service;Username=postgres;Password=***"
}
```

Tạo database `notification_service` trên Postgres (hoặc thêm vào `docs/sql/init-postgres.sql` nếu dùng Docker init).

### 5.3. Entity gợi ý: `NotificationSendLog`

| Cột | Ý nghĩa |
|-----|--------|
| `Id` | PK (Guid) |
| `IdempotencyKey` | **Unique**, ví dụ `OrderCreated:{orderId}`, `OrderCompleted:{orderId}` |
| `EventType` | `OrderCreated`, `OrderCompleted`, `OrderCancelled` |
| `AggregateId` | OrderId (Guid) |
| `RecipientEmail` | nullable nếu skip |
| `Subject` | tiêu đề mail |
| `Status` | `Sent`, `SkippedNoRecipient`, `SkippedNotConfigured`, `Failed` |
| `ErrorDetail` | nullable, ngắn |
| `CreatedAtUtc` | thời điểm ghi |

### 5.4. Luồng xử lý trong handler (pseudo)

1. Tính `idempotencyKey = $"{eventName}:{aggregateId}"`.
2. Nếu đã tồn tại bản ghi với key đó → **return** (đã xử lý / đã gửi).
3. Resolve email + build HTML.
4. Nếu không có email → `INSERT` log `SkippedNoRecipient` với key → return.
5. Gọi SendGrid:
   - Thành công → `INSERT` (hoặc cập nhật) `Sent`.
   - Không cấu hình → `SkippedNotConfigured`.
   - Lỗi provider → `Failed`; **throw** nếu muốn Rabbit retry (cân nhắc vòng lặp).
6. (Tùy chọn) bắt `DbUpdateException` do **unique** trên `IdempotencyKey` để xử lý race hai consumer.

### 5.5. API xem lịch sử

Đã có trong repo: `GET /notifications/dispatches` hoặc `GET /api/notifications/dispatches?take=50` (query `take` tối đa 500).

---

## Đã triển khai trong repo (bước 5)

| File | Nội dung |
|------|----------|
| `NotificationService/Models/NotificationSendLog.cs` | Entity log + idempotency key |
| `NotificationService/Infrastructure/Persistence/NotificationDbContext.cs` | EF Core, bảng `NotificationSendLog`, unique index `IdempotencyKey` |
| `NotificationService/Services/NotificationLedgerService.cs` | Kiểm tra key đã xử lý, gửi mail, ghi log; bắt unique violation khi race |
| `NotificationService/Services/EmailSendOutcome.cs` | Kết quả gửi: Delivered / SkippedNotConfigured / ProviderRejected |
| `NotificationService/Services/NotificationDispatchStatus.cs` | Hằng số trạng thái log |
| `NotificationService/Services/NotificationEmailService.cs` | `SendWithOutcomeAsync` cho ledger |
| `NotificationService/Controllers/NotificationDispatchController.cs` | API lịch sử |
| `docs/sql/init-postgres.sql` | `CREATE DATABASE notification_service;` |

Nếu `ConnectionStrings:NotificationDbPostgres` **không rỗng** → dùng PostgreSQL; ngược lại → SQLite file `notification-service.db`.

---

## Tóm tắt luồng dữ liệu

1. Client tạo đơn → `OrderService` publish `OrderCreated` lên RabbitMQ.
2. `NotificationService` (queue riêng) nhận `OrderCreated` → HTTP lấy email → SendGrid (HTML template).
3. Khi saga thành công → `OrderService` publish `OrderCompleted` → mail hoàn tất.
4. Khi hủy (inventory fail / payment fail → `OrderCancelled`) → mail hủy với `Reason`.
5. **Không** cần subscribe trực tiếp `PaymentFailed` trong Notification.

---

## Checklist triển khai DB

- [x] Package EF + DbContext + `EnsureCreated()` trong `Program.cs`
- [x] Đăng ký `NotificationDbContext` (Postgres nếu có `NotificationDbPostgres`, không thì SQLite)
- [x] `NotificationLedgerService` (scoped): idempotency + log + SendGrid
- [x] `NotificationSagaSubscriber` chỉ gọi ledger
- [ ] Không commit secret SendGrid; dùng User Secrets / biến môi trường trên CI
