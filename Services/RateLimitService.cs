using StackExchange.Redis;

namespace subscription_api.Services;

public class RateLimitService : IRateLimitService
{
    private readonly IDatabase _database;
    private const int MaxRequests = 100;
    private const int WindowSeconds = 60;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<bool> IsRateLimitedAsync(int userId)
    {
        // Fixed window rate limiting with Redis
        // Key format: ratelimit:{userId}:{yyyyMMddHHmm}
        var now = DateTime.UtcNow;
        var window = now.ToString("yyyyMMddHHmm");
        var key = $"ratelimit:{userId}:{window}";

        try
        {
            // INCR: Atomically increment the counter and get the new value
            long count = await _database.StringIncrementAsync(key);

            // EXPIRE: Set expiration only on first request in this window
            // This ensures the key expires after the 1-minute window
            if (count == 1)
            {
                await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(WindowSeconds));
            }

            // Return true if rate limited (exceeded max requests)
            return count > MaxRequests;
        }
        catch
        {
            // On Redis error, allow the request (fail open)
            return false;
        }
    }
}
