using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Dtos;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime SubscripedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public decimal Price { get; set; }
    public bool IsActive => Status == SubscriptionStatus.Active && ExpiresAt > DateTime.UtcNow;
    public int DaysRemaining => (ExpiresAt > DateTime.UtcNow)
                    ? (int)Math.Ceiling((ExpiresAt - DateTime.UtcNow).TotalDays)
                    : 0;
}