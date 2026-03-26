var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var gatewayUrl = builder.Configuration["ApiSettings:GatewayUrl"]
    ?? throw new InvalidOperationException("ApiSettings:GatewayUrl is missing.");
var analyticsUrl = builder.Configuration["ApiSettings:AnalyticsUrl"]
    ?? throw new InvalidOperationException("ApiSettings:AnalyticsUrl is missing.");

builder.Services.AddHttpClient("GatewayClient", client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
});

builder.Services.AddHttpClient("AnalyticsClient", client =>
{
    client.BaseAddress = new Uri(analyticsUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
