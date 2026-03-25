using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationSendLog> NotificationSendLogs => Set<NotificationSendLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationSendLog>(builder =>
        {
            builder.ToTable("NotificationSendLog");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
            builder.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
