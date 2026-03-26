namespace WebFrontend.Models;

public sealed class CustomerCatalogItemDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
