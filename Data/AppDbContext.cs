using Microsoft.EntityFrameworkCore;
using subscription_api.Models;

namespace subscription_api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        // Configure Subscription
        modelBuilder.Entity<Subscription>()
            .HasKey(s => s.UserId);

        modelBuilder.Entity<Subscription>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .IsRequired();
    }
}
