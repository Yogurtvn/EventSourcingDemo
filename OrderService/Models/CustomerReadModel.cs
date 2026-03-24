using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public sealed class CustomerReadModel
{
    [Key]
    [MaxLength(150)]
    public required string CustomerId { get; set; }

    [MaxLength(200)]
    public required string FullName { get; set; }

    [MaxLength(250)]
    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}