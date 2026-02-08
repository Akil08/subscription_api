using Microsoft.EntityFrameworkCore;
using subscription_api.Data;
using subscription_api.Models;

namespace subscription_api.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;

    public SubscriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<Subscription?> GetSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<bool> IncrementUsageAsync(int userId)
    {
        // Atomically increment UsedThisMonth to avoid race conditions
        // Check quota before incrementing
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (subscription == null || subscription.UsedThisMonth >= subscription.MonthlyQuota)
        {
            return false;
        }

        // Use ExecuteUpdateAsync for atomic operation
        var updateCount = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.UsedThisMonth < s.MonthlyQuota)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedThisMonth, x => x.UsedThisMonth + 1));

        return updateCount > 0;
    }

    public async Task UpgradeAsync(int userId, string plan)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (subscription != null)
        {
            subscription.Plan = plan;
            subscription.MonthlyQuota = plan == "Pro" ? 10000 : 1000;
            subscription.UsedThisMonth = 0;
            subscription.SubscriptionEndDate = plan == "Pro" ? DateTime.UtcNow.AddMonths(1) : null;

            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RunDailyJobAsync()
    {
        var today = DateTime.UtcNow.Date;
        var reminderDate = today.AddDays(5);

        // Get all Pro subscriptions
        var proSubscriptions = await _context.Subscriptions
            .Where(s => s.Plan == "Pro")
            .ToListAsync();

        foreach (var sub in proSubscriptions)
        {
            if (sub.SubscriptionEndDate != null)
            {
                var endDate = sub.SubscriptionEndDate.Value.Date;

                // Reminder: If expiring within 5 days
                if (endDate <= reminderDate && endDate > today)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sub.UserId);
                    if (user != null)
                    {
                        Console.WriteLine($"Email reminder sent to {user.Email}");
                    }
                }

                // Downgrade: If subscription expired
                if (endDate <= today)
                {
                    sub.Plan = "Free";
                    sub.MonthlyQuota = 1000;
                    sub.UsedThisMonth = 0;
                    sub.SubscriptionEndDate = null;

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sub.UserId);
                    Console.WriteLine($"User {user?.Email ?? $"ID {sub.UserId}"} downgraded from Pro to Free");

                    _context.Subscriptions.Update(sub);
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
