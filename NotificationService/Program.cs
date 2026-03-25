using NotificationService.Infrastructure.Http;
using NotificationService.Services;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddScoped<OrderCatalogClient>();
builder.Services.AddScoped<NotificationEmailService>();
builder.Services.AddScoped<EmailTemplateService>();

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddHostedService<NotificationSagaSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
