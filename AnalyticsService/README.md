# AnalyticsService

## ?? Mô t?

**AnalyticsService** là m?t microservice trong ki?n trúc Event Sourcing, chuyên x? lý vi?c thu th?p, t?p h?p và phân tích d? li?u t? các s? ki?n c?a h? th?ng. Service này ?óng vai trò quan tr?ng trong vi?c cung c?p insight và metrics cho toàn b? h? th?ng ??t hàng.

### Các ch?c n?ng chính:
- ?? **Thu th?p d? li?u t? events**: Subscribe vào các s? ki?n t? OrderService, InventoryService, PaymentService
- ?? **Xây d?ng Read Models**: T?o và duy trì các mô hình d? li?u t?i ?u cho vi?c query analytics
- ?? **Phân tích th?ng kê**: Cung c?p các API ?? query th?ng kê orders, khách hàng, metrics hàng ngày
- ?? **Tính toán Metrics**: Tính toán completion rate, average order value, revenue, v.v.
- ?? **L?u tr? d? li?u**: Duy trì các read models trong database (PostgreSQL ho?c SQLite)

---

## ??? Ki?n trúc

### Event Sourcing Pattern
AnalyticsService tuân theo mô hình Event Sourcing:

```
OrderService/InventoryService/PaymentService
                    ?
            Publish Events to RabbitMQ
                    ?
            AnalyticsService (Subscriber)
                    ?
            Process & Update Read Models
                    ?
            Database (PostgreSQL/SQLite)
```

### C?u trúc Project

```
AnalyticsService/
??? Controllers/
?   ??? AnalyticsOrdersController.cs      # API cho order analytics
?   ??? AnalyticsCustomersController.cs   # API cho customer analytics
?   ??? AnalyticsMetricsController.cs     # API cho daily metrics
??? Models/
?   ??? OrderAnalyticsReadModel.cs        # Read model cho orders
?   ??? CustomerAnalyticsReadModel.cs     # Read model cho customers
?   ??? DailyOrderMetrics.cs              # Read model cho daily metrics
??? Services/
?   ??? AnalyticsEventService.cs          # X? lý events & c?p nh?t read models
?   ??? AnalyticsEventSubscriber.cs       # Subscribe vào events t? RabbitMQ
??? Infrastructure/
?   ??? Persistence/
?       ??? AnalyticsDbContext.cs         # Entity Framework DbContext
??? Program.cs                             # Startup configuration
??? appsettings.json                       # Configuration
??? README.md                              # Documentation (file này)
```

---

## ??? Database Models

### 1. OrderAnalyticsReadModel
L?u tr? thông tin analytics cho t?ng order.

```csharp
public sealed class OrderAnalyticsReadModel
{
    public Guid OrderId { get; set; }           // Primary Key
    public string CustomerId { get; set; }       // Foreign Key to Customer
    public decimal TotalAmount { get; set; }     // T?ng ti?n ??n hàng
    public string Status { get; set; }           // Created, Completed, Cancelled
    public DateTime CreatedAt { get; set; }      // Th?i gian t?o order
    public DateTime? CompletedAt { get; set; }   // Th?i gian hoàn thành
    public DateTime? CancelledAt { get; set; }   // Th?i gian h?y
    public int DaysToCompletion { get; set; }    // S? ngày ?? hoàn thành
}
```

**Indices**: CustomerId, Status, CreatedAt (?? t?i ?u query)

### 2. CustomerAnalyticsReadModel
L?u tr? thông tin analytics cho t?ng khách hàng.

```csharp
public sealed class CustomerAnalyticsReadModel
{
    public string CustomerId { get; set; }       // Primary Key
    public int TotalOrders { get; set; }         // T?ng s? orders
    public int CompletedOrders { get; set; }     // S? orders hoàn thành
    public int CancelledOrders { get; set; }     // S? orders b? h?y
    public decimal TotalSpent { get; set; }      // T?ng ti?n chi tiêu
    public decimal AverageOrderValue { get; set; } // Giá tr? ??n hàng trung bình
    public DateTime LastOrderDate { get; set; }  // Ngày order cu?i cùng
    public DateTime FirstOrderDate { get; set; } // Ngày order ??u tiên
}
```

**Indices**: TotalOrders, TotalSpent (?? t?i ?u sorting)

### 3. DailyOrderMetrics
L?u tr? metrics t?ng h?p theo ngày.

```csharp
public sealed class DailyOrderMetrics
{
    public DateTime Date { get; set; }              // Primary Key (Date only)
    public int TotalOrdersCreated { get; set; }     // T?ng orders t?o trong ngày
    public int TotalOrdersCompleted { get; set; }   // T?ng orders hoàn thành trong ngày
    public int TotalOrdersCancelled { get; set; }   // T?ng orders h?y trong ngày
    public decimal TotalRevenue { get; set; }       // T?ng doanh thu trong ngày
    public decimal AverageOrderValue { get; set; }  // Giá tr? ??n hàng trung bình
    public double CompletionRate { get; set; }      // T? l? hoàn thành (%)
}
```

---

## ?? Events ???c Subscribe

AnalyticsService subscribe vào các events sau t? RabbitMQ:

### 1. OrderCreated
```csharp
public record OrderCreated(
    Guid AggregateId,           // Order ID
    string CustomerId,          // Customer ID
    decimal TotalAmount,        // Total amount
    string? TestScenario        // Optional test scenario
) : Event(AggregateId);
```
**Hành ??ng**: T?o m?i OrderAnalyticsReadModel, c?p nh?t CustomerAnalyticsReadModel, tính toán DailyMetrics

### 2. OrderCompleted
```csharp
public record OrderCompleted(
    Guid AggregateId,           // Order ID
    string Message              // Completion message
) : Event(AggregateId);
```
**Hành ??ng**: C?p nh?t status thành "Completed", c?p nh?t CompletedAt, tính toán DaysToCompletion

### 3. OrderCancelled
```csharp
public record OrderCancelled(
    Guid AggregateId,           // Order ID
    string Reason               // Cancellation reason
) : Event(AggregateId);
```
**Hành ??ng**: C?p nh?t status thành "Cancelled", c?p nh?t CancelledAt

---

## ?? API Endpoints

### AnalyticsOrdersController (`/api/analytics-orders`)

#### 1. L?y th?ng kê chung v? orders
```http
GET /api/analytics-orders/statistics
```
**Response**:
```json
{
  "totalOrders": 150,
  "completedOrders": 120,
  "cancelledOrders": 10,
  "createdOrders": 20,
  "totalRevenue": 50000,
  "averageOrderValue": 333.33,
  "completionRate": 80,
  "cancellationRate": 6.67
}
```

#### 2. L?y orders theo kho?ng th?i gian
```http
GET /api/analytics-orders/by-date-range?startDate=2024-01-01&endDate=2024-01-31
```
**Response**: M?ng `OrderAnalyticsReadModel`

#### 3. L?y orders theo tr?ng thái
```http
GET /api/analytics-orders/by-status/Completed
```
**Response**: M?ng `OrderAnalyticsReadModel` v?i status = "Completed"

#### 4. L?y chi ti?t analytics c?a m?t order
```http
GET /api/analytics-orders/{orderId}
```
**Response**: `OrderAnalyticsReadModel`

---

### AnalyticsCustomersController (`/api/analytics-customers`)

#### 1. L?y analytics c?a t?t c? khách hàng
```http
GET /api/analytics-customers
```
**Response**: M?ng `CustomerAnalyticsReadModel` (s?p x?p theo TotalSpent gi?m d?n)

#### 2. L?y top khách hàng chi tiêu nhi?u nh?t
```http
GET /api/analytics-customers/top-spenders?limit=10
```
**Response**: M?ng 10 `CustomerAnalyticsReadModel` hàng ??u

#### 3. L?y analytics c?a m?t khách hàng
```http
GET /api/analytics-customers/{customerId}
```
**Response**: `CustomerAnalyticsReadModel`

#### 4. L?y t?t c? orders c?a m?t khách hàng
```http
GET /api/analytics-customers/{customerId}/orders
```
**Response**: M?ng `OrderAnalyticsReadModel` c?a khách hàng

---

### AnalyticsMetricsController (`/api/analytics-metrics`)

#### 1. L?y metrics c?a m?t ngày c? th?
```http
GET /api/analytics-metrics/daily/2024-01-15
```
**Response**: `DailyOrderMetrics`

#### 2. L?y metrics cho kho?ng th?i gian
```http
GET /api/analytics-metrics/daily-range?startDate=2024-01-01&endDate=2024-01-31
```
**Response**: M?ng `DailyOrderMetrics`

#### 3. L?y metrics t?ng h?p cho kho?ng th?i gian
```http
GET /api/analytics-metrics/aggregated-range?startDate=2024-01-01&endDate=2024-01-31
```
**Response**:
```json
{
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "totalOrdersCreated": 150,
  "totalOrdersCompleted": 120,
  "totalOrdersCancelled": 10,
  "totalRevenue": 50000,
  "averageOrderValue": 333.33,
  "averageCompletionRate": 80,
  "daysInRange": 31
}
```

---

## ?? C?u hình và kh?i ??ng

### 1. Dependencies
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.9" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
</ItemGroup>
```

### 2. Connection Strings (`appsettings.json`)

**V?i SQLite** (Default):
```json
{
  "ConnectionStrings": {
    "AnalyticsDb": "Data Source=analytics.db"
  }
}
```

**V?i PostgreSQL**:
```json
{
  "ConnectionStrings": {
    "AnalyticsDbPostgres": "Host=localhost;Port=5432;Database=analytics;Username=postgres;Password=yourpassword"
  }
}
```

### 3. RabbitMQ Configuration
```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

### 4. Ch?y Service
```bash
dotnet run
```

Service s?:
- ? T?o database (n?u ch?a t?n t?i)
- ? Ch?y migrations
- ? Subscribe vào RabbitMQ events
- ? Kh?i ??ng HTTP server t?i `https://localhost:5001`

---

## ?? Event Flow

### Scenario: Order ???c t?o m?i

1. **OrderService** phát `OrderCreated` event
   ```json
   {
     "aggregateId": "550e8400-e29b-41d4-a716-446655440000",
     "customerId": "CUST-001",
     "totalAmount": 100.00,
     "timestamp": "2024-01-15T10:30:00Z"
   }
   ```

2. **AnalyticsService** nh?n event qua RabbitMQ
   - `AnalyticsEventSubscriber` g?i `AnalyticsEventService.HandleOrderCreatedAsync()`

3. **AnalyticsEventService** x? lý:
   - ? T?o `OrderAnalyticsReadModel` m?i
   - ? C?p nh?t `CustomerAnalyticsReadModel`
   - ? C?p nh?t `DailyOrderMetrics` cho ngày hôm nay

4. **API** tr? v? d? li?u t? read models:
   ```bash
   GET /api/analytics-orders/550e8400-e29b-41d4-a716-446655440000
   ```

---

## ?? Query Examples

### Ví d? 1: L?y doanh thu trong tu?n này
```bash
curl "http://localhost:5001/api/analytics-metrics/aggregated-range?startDate=2024-01-08&endDate=2024-01-14"
```

### Ví d? 2: L?y top 5 khách hàng chi tiêu nhi?u nh?t
```bash
curl "http://localhost:5001/api/analytics-customers/top-spenders?limit=5"
```

### Ví d? 3: L?y t?t c? orders c?a khách hàng CUST-001
```bash
curl "http://localhost:5001/api/analytics-customers/CUST-001/orders"
```

### Ví d? 4: L?y th?ng kê t?ng h?p
```bash
curl "http://localhost:5001/api/analytics-orders/statistics"
```

---

## ?? X? lý l?i và Logging

### Error Handling
T?t c? controllers và services có error handling:
```csharp
try
{
    // Business logic
}
catch (Exception ex)
{
    logger.LogError(ex, "Error message");
    return StatusCode(500, new { error = ex.Message });
}
```

### Logging
Service s? d?ng `ILogger` v?i structured logging:
- **Information**: Các ho?t ??ng bình th??ng (events processed, queries executed)
- **Warning**: D? li?u không tìm th?y
- **Error**: Các l?i b?t ng?

**Ví d?**:
```
[14:30:25 INF] OrderCreated analytics recorded for OrderId 550e8400-e29b-41d4-a716-446655440000
[14:30:26 INF] Retrieved 12 orders for date range 2024-01-01 - 2024-01-31
[14:30:27 WRN] Order analytics not found for OrderId 12345678-1234-1234-1234-123456789012
[14:30:28 ERR] Error retrieving order statistics
```

---

## ?? Best Practices

### 1. **Database Indexing**
- ? `OrderId` indexed cho quick lookups
- ? `CustomerId` indexed ?? query theo khách hàng
- ? `Status` indexed ?? filter by status
- ? `CreatedAt` indexed ?? range queries

### 2. **Performance**
- ? Async/await cho t?t c? database operations
- ? LINQ queries ???c d?ch thành SQL queries hi?u qu?
- ? Batch updates cho metrics calculations

### 3. **Scalability**
- ? Read models tách bi?t t? event store
- ? Có th? scale read models ??c l?p
- ? Không lock shared resources

### 4. **Data Consistency**
- ? Eventual consistency model (phù h?p v?i event sourcing)
- ? Read models c?p nh?t asynchronously t? events
- ? Database transactions cho m?i event

---

## ?? Testing

### Unit Tests có th? test:
```csharp
[Test]
public async Task HandleOrderCreated_ShouldCreateOrderAnalytics()
{
    // Arrange
    var orderId = Guid.NewGuid();
    var @event = new OrderCreated(orderId, "CUST-001", 100m, null);
    
    // Act
    await service.HandleOrderCreatedAsync(@event);
    
    // Assert
    var order = await dbContext.OrderAnalytics.FindAsync(orderId);
    Assert.NotNull(order);
    Assert.AreEqual("Created", order.Status);
}
```

---

## ?? Troubleshooting

### Problem: Service không subscribe vào events
**Solution**:
- ? Ki?m tra RabbitMQ có ?ang ch?y
- ? Ki?m tra connection string trong `appsettings.json`
- ? Ki?m tra `AnalyticsEventSubscriber` ???c registered trong DI container

### Problem: Database không ???c t?o
**Solution**:
- ? Ki?m tra connection string
- ? Ki?m tra directory permissions
- ? Ch?y l?i service v?i permission cao h?n

### Problem: Metrics không c?p nh?t
**Solution**:
- ? Ki?m tra logs ?? xem có errors khi processing events
- ? Ki?m tra events có ???c publish t? OrderService
- ? Ki?m tra RabbitMQ message queues

---

## ?? Tham kh?o

- [Microsoft Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Event Sourcing Pattern](https://martinfowler.com/eaaDev/EventSourcing.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)

---

## ?? License

Này là m?t ph?n c?a Event Sourcing Demo project.

---

## ????? Contributors

- Created as part of PRN232 Event Sourcing course project
- Repository: https://github.com/Yogurtvn/EventSourcingDemo

---

**Last Updated**: January 2024  
**Version**: 1.0.0
