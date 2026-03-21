using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ??c c?u hình t? file ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ??ng ký service Ocelot
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// S? d?ng middleware c?a Ocelot
await app.UseOcelot();

app.Run();