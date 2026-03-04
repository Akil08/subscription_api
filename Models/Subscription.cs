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
  
    // but does not usually navigation are set in the fulent api ?
    // Navigation properties can be set using data annotations like [ForeignKey] or 
    // through Fluent API in the DbContext's OnModelCreating method.

    public virtual User User { get; set; } // Navigation Property

}
