using SmartMarket.Domain.Common.Interfaces;

namespace SmartMarket.Domain.Events;

public record SubscriptionExpiredDomainEvent(Guid SubscriptionId, Guid StoreId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}