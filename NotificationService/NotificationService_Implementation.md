# Mô tả Notification Service

## Vai trò trong hệ thống

**Notification Service** là một microservice **quan sát (observer)** bên lề luồng Saga chính. Nó không ra quyết định nghiệp vụ (không giữ hàng, không thanh toán, không đổi trạng thái đơn trong Order Service). Nhiệm vụ của nó là:

- **Nhận** các sự kiện đã được publish lên **RabbitMQ** (cùng exchange `saga.events` với Order / Inventory / Payment).
- **Tra cứu** địa chỉ email khách hàng bằng **HTTP** gọi sang **Order Service** (read model khách hàng và đơn hàng).
- **Gửi email** HTML qua **SendGrid**.
- **Ghi nhận** mỗi lần xử lý vào **cơ sở dữ liệu riêng** để **audit** và **idempotency** (tránh gửi trùng khi broker retry message).

Service chạy như một ứng dụng ASP.NET Core độc lập (cổng mặc định dev khác Order Service, ví dụ `5180`).

---

## Luồng dữ liệu tổng quan

```text
OrderService (và các service khác)
        │  publish JSON event
        ▼
   RabbitMQ (exchange: saga.events, routing key = tên class event)
        │
        ├──► InventoryService / PaymentService / OrderService (Saga)
        │
        └──► NotificationService (queue riêng từng loại event)
                    │
                    ├─► HTTP → OrderService: /orders/customers, /orders/{orderId}
                    ├─► SendGrid API (email HTML)
                    └─► DB: bảng NotificationSendLog
```

Notification **không** đọc Event Store của Order Service và **không** subscribe trực tiếp `PaymentFailed`. Khi thanh toán lỗi, **Order Service** đã chuẩn hóa kết quả thành **`OrderCancelled`**; Notification chỉ cần phản ứng với `OrderCancelled` để gửi mail hủy đơn kèm `Reason`.

---

## Sự kiện được xử lý

| Event (Shared.Contracts) | Hàng đợi RabbitMQ (ví dụ) | Nội dung email (ý tưởng) |
|--------------------------|---------------------------|---------------------------|
| `OrderCreated` | `notification.order-created` | Xác nhận đã nhận đơn, tổng tiền |
| `OrderCompleted` | `notification.order-completed` | Đơn hoàn tất / thanh toán thành công |
| `OrderCancelled` | `notification.order-cancelled` | Đơn bị hủy và lý do |

Payload event mang `AggregateId` (order id) và các field theo contract; **email khách** không có trong message, nên service phải gọi Order Service.

---

## Thành phần chính trong codebase

| Thành phần | Trách nhiệm |
|------------|-------------|
| **NotificationSagaSubscriber** | `IHostedService`: khi app khởi động đăng ký `IRabbitMqEventBus.Subscribe<...>`; mỗi handler tạo DI scope và gọi **NotificationLedgerService**. |
| **NotificationLedgerService** | Tính **idempotency key** (`OrderCreated:{guid}`, …); nếu đã có bản ghi trong DB thì bỏ qua; resolve email; gọi template + SendGrid; **ghi một dòng** `NotificationSendLog`; bắt vi phạm unique khi hai consumer race hiếm. |
| **OrderCatalogClient** | HTTP client: danh sách khách (`/orders/customers`), chi tiết đơn (`/orders/{orderId}`) để suy ra `CustomerId` → email. |
| **NotificationEmailService** | Gọi SendGrid; trả về **EmailSendOutcome** (gửi thành công / chưa cấu hình / provider từ chối). Exception mạng vẫn có thể ném ra để Rabbit retry theo cấu hình bus. |
| **EmailTemplateService** | Sinh nội dung HTML (layout + nội dung theo từng loại thông báo). |
| **NotificationDbContext** + **NotificationSendLog** | EF Core; bảng `NotificationSendLog`, **unique** trên `IdempotencyKey`. |
| **NotificationStatusController** | `GET /notifications/health` — kiểm tra service sống. |
| **NotificationDispatchController** | `GET /notifications/dispatches?take=…` — đọc lịch sử gửi (tối đa `take` theo giới hạn API). |

**Giao thức messaging:** `IRabbitMqEventBus` chỉ có `Subscribe<TEvent>(queueName, handler)`, không có `SubscribeAsync`. Exchange phải trùng các service còn lại (**`saga.events`**).

---

## Idempotency và trạng thái log

Mỗi cặp (loại event + `AggregateId`) ứng với một **IdempotencyKey** duy nhất. Trước khi gửi, service kiểm tra đã tồn tại key trong DB chưa; nếu có thì **không gửi lại** (phù hợp khi message bị deliver lại).

Sau khi xử lý, một dòng log ghi:

- **Sent** — SendGrid trả thành công.
- **SkippedNoRecipient** — không tìm được email.
- **SkippedNotConfigured** — thiếu `SendGrid:ApiKey` / `FromEmail`.
- **Failed** — SendGrid trả mã lỗi HTTP (không ném exception trong trường hợp đó để message được ack; có thể điều chỉnh sau nếu muốn retry).

---

## Cấu hình (tóm tắt)

- **OrderService:BaseUrl** — URL gốc Order API (mặc định dev thường `http://localhost:5044`).
- **ConnectionStrings** — `NotificationDb` (SQLite) hoặc `NotificationDbPostgres` (PostgreSQL, database `notification_service` nếu dùng Postgres; có thể tạo qua `docs/sql/init-postgres.sql` ở solution).
- **RabbitMq** — giống các service khác (`HostName`, `Port`, `ExchangeName`, …).
- **SendGrid** — API key và sender đã xác thực; **không nên** commit secret vào Git; dùng User Secrets hoặc biến môi trường trên môi trường thật.

---

## Phụ thuộc vận hành

Để Notification hoạt động đầy đủ cần:

1. **RabbitMQ** đang chạy và các service Saga publish event.
2. **Order Service** chạy và API customers/orders phản hồi đúng.
3. **SendGrid** (hoặc chấp nhận log `SkippedNotConfigured` khi dev không cấu hình).
4. **Database** — SQLite file hoặc Postgres; schema tạo khi khởi động (`EnsureCreated`).

---

## Tóm tắt

Notification Service **bổ sung trải nghiệm người dùng** (email) và **minh chứng gửi** (DB), **tách biệt** với luồng nghiệp vụ Saga: nó phản ứng với các event đã “chốt” hoặc đã được định nghĩa rõ (`OrderCreated`, `OrderCompleted`, `OrderCancelled`) thay vì can thiệp vào từng bước trung gian như `InventoryReserved` hay `PaymentFailed`.
