using Microsoft.EntityFrameworkCore;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Services;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("AnalyticsDbPostgres");
var sqliteConnection = builder.Configuration.GetConnectionString("AnalyticsDb");

builder.Services.AddDbContext<AnalyticsDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        options.UseNpgsql(postgresConnection);
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddScoped<AnalyticsEventService>();
builder.Services.AddHostedService<AnalyticsEventSubscriber>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
