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
   
        //  what exactly mean by atokmic operation here?
        // An atomic operation is a sequence of operations that are indivisible, 
        // meaning that they either complete entirely or not at all.

        // for any executeupdateasync, it will alwasy be atomic ? 
        // Yes, ExecuteUpdateAsync is designed to perform atomic updates on the database. 
        // It ensures that the update operation is executed as a single unit of work,
        // meaning that if any part of the update fails, the entire operation will be rolled back,
        // maintaining data integrity and consistency in the database.

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
            // make date for both, not for pro only
            subscription.SubscriptionEndDate =  DateTime.UtcNow.AddMonths(1) ; 

            // if not pro plan, then endate null ? 
            // Yes, if the plan is not "Pro", then the SubscriptionEndDate should be set to null, 
            // indicating that there is no expiration date for the subscription. 
            // This is because the "Free" plan does not have a time limit, while the "Pro" plan does.
            // but both plan have monthly quota, 
            // so if user downgrade from pro to free, 
            // then the enddate should be null but the monthly quota should be 1000 and 
            // usedthismonth should be reset to 0 ?
            // Yes, when a user downgrades from "Pro" to "Free", the SubscriptionEndDate should be set to null,
            // the MonthlyQuota should be set to 1000, and the UsedThisMonth should be reset to 0.
            // This ensures that the user has a fresh start with the "Free" plan, 
            // and there is no expiration date associated with the subscription.


            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }
 
   
    

public async Task RunDailyJobAsync()
{
    var today = DateTime.UtcNow.Date;
    var reminderDate = today.AddDays(5);

    // 1. Bulk Reset Free Subscriptions (One SQL Command)
    await _context.Subscriptions
        .Where(s => s.Plan == "Free" && s.SubscriptionEndDate <= today)
        .ExecuteUpdateAsync(s => s
            .SetProperty(b => b.UsedThisMonth, 0)
            .SetProperty(b => b.SubscriptionEndDate, DateTime.UtcNow.AddMonths(1)));

    // 2. Optimized Pro Reminders (Fetch only needed data)
    var expiringProSubs = await _context.Subscriptions
        .Include(s => s.User) // Join with User table in ONE query
        .Where(s => s.Plan == "Pro" && s.SubscriptionEndDate != null 
                   && s.SubscriptionEndDate.Value.Date <= reminderDate 
                   && s.SubscriptionEndDate.Value.Date > today)
        .ToListAsync();

    foreach (var sub in expiringProSubs)
    {
        Console.WriteLine($"Email reminder sent to {sub.User.Email}");
    }

    // 3. Bulk Downgrade Expired Pro (One SQL Command)
    await _context.Subscriptions
        .Where(s => s.Plan == "Pro" && s.SubscriptionEndDate != null && s.SubscriptionEndDate.Value.Date <= today)
        .ExecuteUpdateAsync(s => s
            .SetProperty(b => b.Plan, "Free")
            .SetProperty(b => b.MonthlyQuota, 1000)
            .SetProperty(b => b.UsedThisMonth, 0)
            .SetProperty(b => b.SubscriptionEndDate, (DateTime?)null));

    // No need for separate SaveChangesAsync as ExecuteUpdateAsync handles it internally

}
}