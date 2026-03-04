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
            .WithMany() // why many ? 
            // The WithMany() method is used to configure a one-to-many relationship 
            // between the Subscription and User entities.
            // In this case, it indicates that a User can have many Subscriptions, 
            // but a Subscription is associated with only one User.
            .HasForeignKey(s => s.UserId)
            .IsRequired();
    }
}
