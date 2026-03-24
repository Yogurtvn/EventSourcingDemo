using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using OrderService.Infrastructure.Persistence;
using OrderService.Services;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("OrderDbPostgres");
var sqliteConnection = builder.Configuration.GetConnectionString("OrderDb");

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        options.UseNpgsql(postgresConnection);
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddScoped<OrderProjector>();
builder.Services.AddScoped<OrderEventService>();
builder.Services.AddHostedService<OrderReadModelBootstrapper>();
builder.Services.AddHostedService<OrderSagaSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();

    // Ensure customer catalog table exists even when database already had legacy tables.
    if (dbContext.Database.IsNpgsql())
    {
        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""CustomerReadModel"" (
    ""CustomerId"" character varying(150) PRIMARY KEY,
    ""FullName"" character varying(200) NOT NULL,
    ""Email"" character varying(250),
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp without time zone NOT NULL
);");
    }
    else
    {
        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""CustomerReadModel"" (
    ""CustomerId"" TEXT NOT NULL PRIMARY KEY,
    ""FullName"" TEXT NOT NULL,
    ""Email"" TEXT NULL,
    ""IsActive"" INTEGER NOT NULL,
    ""CreatedAt"" TEXT NOT NULL
);");
    }

    if (!dbContext.Customers.Any())
    {
        dbContext.Customers.AddRange(
            new CustomerReadModel
            {
                CustomerId = "CUST-001",
                FullName = "Nguyen Van An",
                Email = "an@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CustomerReadModel
            {
                CustomerId = "CUST-002",
                FullName = "Tran Thi Binh",
                Email = "binh@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CustomerReadModel
            {
                CustomerId = "CUST-003",
                FullName = "Le Quang Minh",
                Email = "minh@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        dbContext.SaveChanges();
    }
}

app.MapControllers();

app.Run();
