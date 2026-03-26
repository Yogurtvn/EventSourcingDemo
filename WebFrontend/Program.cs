var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Đăng ký HttpClient với BaseAddress trỏ thẳng vào API Gateway
builder.Services.AddHttpClient("GatewayClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayUrl"]);
});

// Client dùng để Đọc báo cáo (Luồng Read - gọi thẳng Service chuẩn CQRS)
builder.Services.AddHttpClient("AnalyticsClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:AnalyticsUrl"]!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
