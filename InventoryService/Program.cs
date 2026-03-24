using InventoryService.Infrastructure.Persistence;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("InventoryDbPostgres");
var sqliteConnection = builder.Configuration.GetConnectionString("InventoryDb");

builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        options.UseNpgsql(postgresConnection);
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddScoped<InventoryProjector>();
builder.Services.AddScoped<InventoryEventService>();
builder.Services.AddHostedService<InventoryReadModelBootstrapper>();
builder.Services.AddHostedService<InventorySagaSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

app.Run();
