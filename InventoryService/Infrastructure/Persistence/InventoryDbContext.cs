using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<InventoryReservationReadModel> Reservations => Set<InventoryReservationReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStoreEntry>(builder =>
        {
            builder.ToTable("InventoryEventStore");
            builder.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
            builder.Property(x => x.EventData).HasColumnType("TEXT");
        });

        modelBuilder.Entity<InventoryReservationReadModel>(builder =>
        {
            builder.ToTable("InventoryReservationReadModel");
            builder.HasKey(x => x.OrderId);
        });
    }
}
