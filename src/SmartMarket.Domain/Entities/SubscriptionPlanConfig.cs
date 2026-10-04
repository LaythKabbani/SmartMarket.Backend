using SmartMarket.Domain.Enums;

namespace SmartMarket.Domain.Entities;

public class SubscriptionPlanConfig : BaseEntity
{
    public SubscriptionPlan PlanType { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";

    private SubscriptionPlanConfig() { } // For EF Core

    public SubscriptionPlanConfig(SubscriptionPlan plan, decimal price, string currency)
    {
        PlanType = plan;
        Price = price;
        Currency = currency;
        Name = plan.ToString();
    }
}