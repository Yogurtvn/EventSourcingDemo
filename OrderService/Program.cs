var builder = WebApplication.CreateBuilder(args);


// ??ng ký EventStore là Singleton ?? d? li?u không b? m?t m?i khi g?i API
builder.Services.AddSingleton<OrderService.Data.IEventStore, OrderService.Data.InMemoryEventStore>();

// ??ng ký HttpClient ?? OrderService có th? "b?n" s? ki?n sang service khác
builder.Services.AddHttpClient();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
