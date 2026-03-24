using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<OrderReadModel> Orders => Set<OrderReadModel>();
    public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStoreEntry>(builder =>
        {
            builder.ToTable("OrderEventStore");
            builder.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
            builder.Property(x => x.EventData).HasColumnType("TEXT");
        });

        modelBuilder.Entity<OrderReadModel>(builder =>
        {
            builder.ToTable("OrderReadModel");
            builder.HasKey(x => x.OrderId);
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<CustomerReadModel>(builder =>
        {
            builder.ToTable("CustomerReadModel");
            builder.HasKey(x => x.CustomerId);
        });
    }
}
