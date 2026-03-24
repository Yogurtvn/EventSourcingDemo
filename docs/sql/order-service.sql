-- Order Service schema (Event Sourcing)

CREATE TABLE OrderEventStore (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    AggregateId UNIQUEIDENTIFIER NOT NULL,
    EventType VARCHAR(200) NOT NULL,
    EventData TEXT NOT NULL,
    TimestampUtc DATETIME2 NOT NULL,
    Version INT NOT NULL,
    CONSTRAINT UQ_OrderEventStore_Aggregate_Version UNIQUE (AggregateId, Version)
);

CREATE INDEX IX_OrderEventStore_AggregateId ON OrderEventStore (AggregateId);
CREATE INDEX IX_OrderEventStore_TimestampUtc ON OrderEventStore (TimestampUtc);

CREATE TABLE OrderReadModel (
    OrderId UNIQUEIDENTIFIER PRIMARY KEY,
    CustomerId VARCHAR(150) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    LastUpdatedAt DATETIME2 NOT NULL
);

-- Read model duoc build/rebuild tu OrderEventStore.
-- Khong duoc coi OrderReadModel la source of truth.
