namespace subscription_api.Models;

public class Subscription
{
    public int UserId { get; set; }
    public string Plan { get; set; } = "Free"; // "Free" or "Pro"
    public int MonthlyQuota { get; set; }
    public int UsedThisMonth { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}
