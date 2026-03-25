using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Http;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Services;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("NotificationDbPostgres");
var sqliteConnection = builder.Configuration.GetConnectionString("NotificationDb");

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        options.UseNpgsql(postgresConnection);
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<OrderCatalogClient>();
builder.Services.AddScoped<NotificationEmailService>();
builder.Services.AddScoped<EmailTemplateService>();
builder.Services.AddScoped<NotificationLedgerService>();

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddHostedService<NotificationSagaSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

app.Run();
