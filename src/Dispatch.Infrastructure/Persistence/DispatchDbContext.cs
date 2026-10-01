using Dispatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dispatch.Infrastructure.Persistence;

public sealed class DispatchDbContext : DbContext
{
    public DispatchDbContext(DbContextOptions<DispatchDbContext> options) : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).HasMaxLength(5000);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.Channel).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.IsSent);
        });
    }
}
