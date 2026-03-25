using Microsoft.EntityFrameworkCore;
using AnalyticsService.Models;

namespace AnalyticsService.Infrastructure.Persistence;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<OrderAnalyticsReadModel> OrderAnalytics => Set<OrderAnalyticsReadModel>();
    public DbSet<DailyOrderMetrics> DailyMetrics => Set<DailyOrderMetrics>();
    public DbSet<CustomerAnalyticsReadModel> CustomerAnalytics => Set<CustomerAnalyticsReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderAnalyticsReadModel>(builder =>
        {
            builder.ToTable("OrderAnalytics");
            builder.HasKey(x => x.OrderId);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<DailyOrderMetrics>(builder =>
        {
            builder.ToTable("DailyOrderMetrics");
            builder.HasKey(x => x.Date);
        });

        modelBuilder.Entity<CustomerAnalyticsReadModel>(builder =>
        {
            builder.ToTable("CustomerAnalytics");
            builder.HasKey(x => x.CustomerId);
            builder.HasIndex(x => x.TotalOrders);
            builder.HasIndex(x => x.TotalSpent);
        });
    }
}
