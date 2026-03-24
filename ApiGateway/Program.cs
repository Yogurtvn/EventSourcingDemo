var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
	app.MapGet("/", () => Results.Ok(new { Status = "GatewayUp" }));
}

app.MapGet("/gateway/health", () => Results.Ok(new { Status = "GatewayUp" }));

app.MapPost("/gateway/orders", async (
	CreateOrderRequest request,
	IHttpClientFactory httpClientFactory,
	IConfiguration configuration,
	CancellationToken cancellationToken) =>
{
	var orderServiceBase = configuration["ReverseProxy:Clusters:order-cluster:Destinations:order-api:Address"];
	if (string.IsNullOrWhiteSpace(orderServiceBase))
	{
		return Results.Problem("Order service address is not configured.");
	}

	var client = httpClientFactory.CreateClient();
	using var response = await client.PostAsJsonAsync(
		$"{orderServiceBase.TrimEnd('/')}/orders",
		request,
		cancellationToken);

	var content = await response.Content.ReadAsStringAsync(cancellationToken);
	if (!response.IsSuccessStatusCode)
	{
		return Results.Problem($"Order service returned {(int)response.StatusCode}: {content}");
	}

	return Results.Content(content, "application/json");
})
.WithName("CreateOrderFromGateway");

app.MapReverseProxy();

app.Run();

public sealed record CreateOrderRequest(string CustomerId, decimal TotalAmount, string? TestScenario);
