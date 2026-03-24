using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Services;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("PaymentDbPostgres");
var sqliteConnection = builder.Configuration.GetConnectionString("PaymentDb");

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        options.UseNpgsql(postgresConnection);
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddScoped<PaymentProjector>();
builder.Services.AddScoped<PaymentEventService>();
builder.Services.AddHostedService<PaymentReadModelBootstrapper>();
builder.Services.AddHostedService<PaymentSagaSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

app.Run();
