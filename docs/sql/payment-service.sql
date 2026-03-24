-- Payment Service schema (Event Sourcing)

CREATE TABLE PaymentEventStore (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    AggregateId UNIQUEIDENTIFIER NOT NULL,
    EventType VARCHAR(200) NOT NULL,
    EventData TEXT NOT NULL,
    TimestampUtc DATETIME2 NOT NULL,
    Version INT NOT NULL,
    CONSTRAINT UQ_PaymentEventStore_Aggregate_Version UNIQUE (AggregateId, Version)
);

CREATE INDEX IX_PaymentEventStore_AggregateId ON PaymentEventStore (AggregateId);
CREATE INDEX IX_PaymentEventStore_TimestampUtc ON PaymentEventStore (TimestampUtc);

CREATE TABLE PaymentReadModel (
    OrderId UNIQUEIDENTIFIER PRIMARY KEY,
    PaymentId UNIQUEIDENTIFIER NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    FailureReason VARCHAR(250) NULL,
    LastUpdatedAt DATETIME2 NOT NULL
);

-- PaymentReadModel la projection tu PaymentEventStore.
