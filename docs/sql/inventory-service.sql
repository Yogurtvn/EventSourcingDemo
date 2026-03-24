-- Inventory Service schema (Event Sourcing)

CREATE TABLE InventoryEventStore (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    AggregateId UNIQUEIDENTIFIER NOT NULL,
    EventType VARCHAR(200) NOT NULL,
    EventData TEXT NOT NULL,
    TimestampUtc DATETIME2 NOT NULL,
    Version INT NOT NULL,
    CONSTRAINT UQ_InventoryEventStore_Aggregate_Version UNIQUE (AggregateId, Version)
);

CREATE INDEX IX_InventoryEventStore_AggregateId ON InventoryEventStore (AggregateId);
CREATE INDEX IX_InventoryEventStore_TimestampUtc ON InventoryEventStore (TimestampUtc);

CREATE TABLE InventoryReservationReadModel (
    OrderId UNIQUEIDENTIFIER PRIMARY KEY,
    ReservationStatus VARCHAR(50) NOT NULL,
    Reason VARCHAR(250) NULL,
    ReservedAt DATETIME2 NULL,
    ReleasedAt DATETIME2 NULL,
    LastUpdatedAt DATETIME2 NOT NULL
);

-- Projection tu InventoryEventStore, phuc vu query trang thai reserve/release.
