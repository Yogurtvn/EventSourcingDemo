using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<PaymentReadModel> Payments => Set<PaymentReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStoreEntry>(builder =>
        {
            builder.ToTable("PaymentEventStore");
            builder.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
            builder.Property(x => x.EventData).HasColumnType("TEXT");
        });

        modelBuilder.Entity<PaymentReadModel>(builder =>
        {
            builder.ToTable("PaymentReadModel");
            builder.HasKey(x => x.OrderId);
        });
    }
}
