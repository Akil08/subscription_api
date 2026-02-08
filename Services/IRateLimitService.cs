namespace subscription_api.Services;

public interface IRateLimitService
{
    Task<bool> IsRateLimitedAsync(int userId);
}
