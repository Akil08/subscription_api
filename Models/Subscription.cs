namespace subscription_api.Models;
using System.ComponentModel.DataAnnotations.Schema;
public class Subscription
{
    public int UserId { get; set; }
    public string Plan { get; set; } = "Free"; // "Free" or "Pro"
    public int MonthlyQuota { get; set; }
    public int UsedThisMonth { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }

    [ForeignKey("UserId")]
  
    public virtual User User { get; set; } // Navigation Property

}
