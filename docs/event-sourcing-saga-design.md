# Event Sourcing + Saga (Choreography) Design

## 1) Flow + Event

### Danh sach event
- OrderCreated
- InventoryReserved
- InventoryFailed
- PaymentSucceeded
- PaymentFailed
- InventoryReleased
- OrderCompleted
- OrderCancelled

### Publisher va Subscriber
| Event | Publisher | Subscriber |
|---|---|---|
| OrderCreated | Order Service | Inventory Service |
| InventoryReserved | Inventory Service | Payment Service |
| InventoryFailed | Inventory Service | Order Service |
| PaymentSucceeded | Payment Service | Order Service |
| PaymentFailed | Payment Service | Order Service, Inventory Service |
| InventoryReleased | Inventory Service | (Audit/Read side) |
| OrderCompleted | Order Service | (Read side / Notification / Analytics) |
| OrderCancelled | Order Service | Inventory Service, (Read side / Notification / Analytics) |

### Happy case
1. Client goi tao don hang -> Order Service ghi event OrderCreated vao EventStore va publish OrderCreated.
2. Inventory Service subscribe OrderCreated, reserve ton kho, ghi InventoryReserved va publish InventoryReserved.
3. Payment Service subscribe InventoryReserved, thanh toan thanh cong, ghi PaymentSucceeded va publish PaymentSucceeded.
4. Order Service subscribe PaymentSucceeded, ghi PaymentSucceeded vao EventStore local.
5. Order Service phat sinh OrderCompleted, ghi vao EventStore va publish OrderCompleted.
6. Read model cua tung service duoc cap nhat bang projection tu event stream.

### Failure case va compensation (khong dung DB transaction)

### Case A: Payment fail sau khi da reserve inventory
1. OrderCreated -> InventoryReserved nhu happy case.
2. Payment Service xu ly thanh toan that bai, publish PaymentFailed.
3. Order Service subscribe PaymentFailed:
   - Ghi PaymentFailed
   - Phat sinh compensation event OrderCancelled va publish.
4. Inventory Service subscribe PaymentFailed, thuc hien compensation release ton kho:
   - Ghi InventoryReleased
   - Publish InventoryReleased.
5. Order Service co the subscribe InventoryReleased de cap nhat read model (neu can).

### Case B: Inventory khong reserve duoc
1. OrderCreated duoc Inventory Service nhan.
2. Inventory Service khong reserve duoc -> publish InventoryFailed voi ly do "InventoryUnavailable".
3. Order Service subscribe InventoryFailed -> phat sinh OrderCancelled (compensation theo saga).

## 2) Event class
- Tat ca event dung base class `Event` voi:
   - `AggregateId`
   - `Timestamp`
- JSON serialize su dung `System.Text.Json` de luu `EventData` vao EventStore.

## 3) Database
- Moi service co 2 table:
   - EventStore (source of truth)
   - ReadModel (projection cho query)
- Khong luu state nghiep vu truc tiep ngoai read model projection.

## 4) Service behavior
- Order Service:
   - Publish: OrderCreated, OrderCompleted, OrderCancelled
   - Subscribe: PaymentSucceeded, PaymentFailed, InventoryFailed
- Inventory Service:
   - Subscribe: OrderCreated, PaymentFailed, OrderCancelled
   - Publish: InventoryReserved, InventoryFailed, InventoryReleased
- Payment Service:
   - Subscribe: InventoryReserved
   - Publish: PaymentSucceeded hoac PaymentFailed (random de test)

## 5) Infrastructure (RabbitMQ)
- Exchange topic: `saga.events`
- Routing key: ten event (`OrderCreated`, `InventoryReserved`, ...)
- Co module dung chung cho publish/subscribe.
- Co retry khi handler fail (`x-retry-count`), log thanh cong/that bai.

## 6) Nguyen tac thiet ke
- Moi service chi luu EventStore la source of truth.
- ReadModel la projection de query, co the rebuild tu EventStore bat ky luc nao.
- Khong lock transaction DB xuyen service.
- Rollback lien service bang compensation event (PaymentFailed -> InventoryReleased, OrderCancelled).
