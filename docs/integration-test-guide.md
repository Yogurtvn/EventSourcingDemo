# Integration Test Guide (Click-Only in VS Code)

## 1) One-time setup

- Open folder: D:/PRN232/Event Sourcing
- Open Run and Debug panel in VS Code.
- Select launch profile: Run All Services + Gateway
- Press Run (F5).

This one click will:
- Ensure RabbitMQ container is running
- Start OrderService (http://localhost:5044)
- Start InventoryService (http://localhost:5077)
- Start PaymentService (http://localhost:5122)
- Start ApiGateway (http://localhost:5214)

## 2) Open Swagger UIs

- Gateway Swagger: http://localhost:5214/swagger
- Order Swagger: http://localhost:5044/swagger
- Inventory Swagger: http://localhost:5077/swagger
- Payment Swagger: http://localhost:5122/swagger
- RabbitMQ UI: http://localhost:15672 (guest/guest)

## 3) API endpoints to test through Gateway

- POST /orders
- GET /orders/{id}
- GET /orders/{id}/events
- GET /inventory/{id}/events
- GET /payments/{id}/events

Base URL for all above: http://localhost:5214

## 4) Happy case test steps

1. In Gateway Swagger, call POST /orders with:
  - customerId: CUST-HAPPY
  - totalAmount: 120
2. Copy orderId from response.
3. Call GET /orders/{id} with the copied id.
4. Call event endpoints:
  - GET /orders/{id}/events
  - GET /inventory/{id}/events
  - GET /payments/{id}/events

Expected:
- Order status = Completed
- Flow = OrderCreated -> InventoryReserved -> PaymentSucceeded -> OrderCompleted

## 5) Failure + compensation rollback test steps

Payment is random success/fail, so create new orders until one gets Cancelled.

1. Repeat POST /orders in Gateway Swagger.
2. For each orderId, call GET /orders/{id}.
3. When status is Cancelled, verify:
  - GET /orders/{id}/events shows OrderCreated -> PaymentFailed -> OrderCancelled
  - GET /inventory/{id}/events shows InventoryReserved -> InventoryReleased
  - GET /payments/{id}/events shows PaymentFailed
4. Verify Inventory read model rollback from Inventory Swagger:
  - GET /api/inventory/{id}

Expected:
- reservationStatus = Released
- releasedAt has value

## 6) Verify EventStore requirements

For each service event endpoint:
- Response is non-empty (event persisted in EventStore)
- version increments in order (1, 2, 3...)
- event order matches saga behavior

## 7) Verify message broker requirements

Check the Output/Terminal logs in VS Code for:
- EVENT PUBLISHED: ...
- EVENT CONSUMED: ...

At minimum, verify:
- OrderCreated published by OrderService and consumed by InventoryService
- InventoryReserved published by InventoryService and consumed by PaymentService

## 8) Stop all running services

- Press Stop in the Run and Debug toolbar.
- This stops all services in the compound profile.
- If you want to stop RabbitMQ container too, run VS Code task named Stop RabbitMQ from the Tasks menu.
