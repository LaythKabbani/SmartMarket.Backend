namespace SmartMarket.Domain.Entities;

public class MerchantSubscription : BaseEntity
{
    public Guid StoreId { get; set; }
    public global::SubscriptionPlan PlanType { get; set; } // Monthly, Yearly, Demo

    public DateTime SubscripedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsActive => DateTime.UtcNow <= ExpiresAt;
    public SubscriptionStatus Status { get; set; } // Active, Expired, Cancelled, PendingPayment

    public virtual Store Store { get; set; } = null!;

    private MerchantSubscription() { } // For EF Core

    public MerchantSubscription(Guid storeId, global::SubscriptionPlan planType, DateTime subscribedAt, DateTime expiresAt, SubscriptionStatus status)
    {
        StoreId = storeId;
        PlanType = planType;
        SubscripedAt = subscribedAt;
        ExpiresAt = expiresAt;
        Status = status;
    }
}