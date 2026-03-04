using Microsoft.AspNetCore.Mvc;
using subscription_api.DTOs;
using subscription_api.Services;

namespace subscription_api.Controllers;

[ApiController]
[Route("api")]
public class SubscriptionController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(IRateLimitService rateLimitService, ISubscriptionService subscriptionService)
    {
        _rateLimitService = rateLimitService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet("data/user/{userId:int}")]


    // why iactionresult , why not action result ?
    // IActionResult is an interface that represents the result of an action method in ASP.NET Core MVC. 
    // It allows you to return different types of responses (e.g., Ok, NotFound, BadRequest)
    //  from your controller actions.
    // ActionResult is a concrete class that implements IActionResult and provides 
    // a more specific way to return responses.
    // You can use IActionResult when you want to return different types of 
    // responses based on certain conditions, while ActionResult is typically 
    // sed when you want to return a specific type of response (e.g., OkObjectResult, NotFoundResult) 
    // directly from your action method.
    
    // can i always replace IActionResult with ActionResult in this method ?
    // No, you cannot always replace IActionResult with ActionResult in this method.
    // IActionResult allows you to return different types of responses (e.g., Ok, NotFound, BadRequest) 
    // based on certain conditions, 
    // while ActionResult is typically used when you want to return a specific type of response directly 
    // from your action method.
    // If your method needs to return different types of responses based on conditions, you should use IActionResult. 
    // If your method always returns a specific type of response, you can use ActionResult for simplicity. 
    // However, in this case, since the method can return different types of responses (Ok, NotFound, StatusCode), 
    // it is more appropriate to use IActionResult.
    // In summary, use IActionResult when you need flexibility in the types of responses you return, 
    // and use ActionResult when you always return a specific type of response.
    public async Task<IActionResult> GetData(int userId)
    {
        // Check if user exists
        var user = await _subscriptionService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Check rate limit
        var isRateLimited = await _rateLimitService.IsRateLimitedAsync(userId);
        if (isRateLimited)
        {  
            // false means they user can make req ? 
            // Yes, if IsRateLimitedAsync returns false, 
            // it means the user is not currently rate limited and can make the request.
            return StatusCode(429, "Too Many Requests");
        }

        // Check monthly quota
        var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
        if (subscription == null || subscription.UsedThisMonth >= subscription.MonthlyQuota)
        {
            return StatusCode(429, "Monthly Quota Exceeded");
        }

        // Increment usage atomically
        var success = await _subscriptionService.IncrementUsageAsync(userId);
        if (!success)
        {
            return StatusCode(429, "Monthly Quota Exceeded");
        }

        return Ok(new { data = "Some API response data" });
    }

    [HttpPost("upgrade/user/{userId:int}")]
    public async Task<IActionResult> Upgrade(int userId, [FromBody] UpgradeRequestDto request)
    {
        // Check if user exists
        var user = await _subscriptionService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Validate plan
        if (request.Plan != "Free" && request.Plan != "Pro")
        {
            return BadRequest("Invalid plan. Must be 'Free' or 'Pro'.");
        }

        // Upgrade/Downgrade
        await _subscriptionService.UpgradeAsync(userId, request.Plan);

        // Get updated subscription
        var subscription = await _subscriptionService.GetSubscriptionAsync(userId);

        return Ok(new
        {
            success = true,
            message = $"Successfully upgraded to {request.Plan} plan",
            monthlyQuota = subscription?.MonthlyQuota,
            usedThisMonth = 0
        });
    }
}
