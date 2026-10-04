using MediatR;
using SmartMarket.Domain.Common;
using SmartMarket.Domain.Common.Interfaces;

namespace SmartMarket.Application.Common.Models;

public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}