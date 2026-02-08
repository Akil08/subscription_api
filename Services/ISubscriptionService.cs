using subscription_api.Models;

namespace subscription_api.Services;

public interface ISubscriptionService
{
    Task<User?> GetUserAsync(int userId);
    Task<Subscription?> GetSubscriptionAsync(int userId);
    Task<bool> IncrementUsageAsync(int userId);
    Task UpgradeAsync(int userId, string plan);
    Task RunDailyJobAsync();
}
