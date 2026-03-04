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
             
            // what if after count the keyexpire is not set  casue of some error ?
            // If the key expiration is not set due to an error, 
            // the counter will continue to increment indefinitely for that user and window,
            //  which could lead to incorrect rate limiting behavior. 

            // so what is the actual solution for this ? lua script ?
            // Yes, using a Lua script in Redis would be a more robust 
            // solution to ensure atomicity of both the increment and expiration operations. 
            // The Lua script would increment the counter and set the 
            // expiration in a single atomic operation,
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
  
            // does  that means if the redis is donw , this catch block will 
            // retrhn false ? 
            // Yes, if Redis is down and an exception is thrown, 
            // the catch block will execute and return false,
            // allowing the request to proceed without rate limiting.

            // ok.

            return false;
        }
    }
}
