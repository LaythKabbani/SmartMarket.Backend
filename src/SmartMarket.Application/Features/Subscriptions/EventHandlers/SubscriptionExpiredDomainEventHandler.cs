using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Domain.Events;
using SmartMarket.Domain.Enums;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Subscriptions.EventHandlers;

public class SubscriptionExpiredDomainEventHandler :
    INotificationHandler<DomainEventNotification<SubscriptionExpiredDomainEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SubscriptionExpiredDomainEventHandler> _logger;
    private readonly IEmailService _emailService;

    public SubscriptionExpiredDomainEventHandler(
        IApplicationDbContext context,
        ILogger<SubscriptionExpiredDomainEventHandler> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Handle(DomainEventNotification<SubscriptionExpiredDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Processing SubscriptionExpiredDomainEvent for Store {StoreId}", domainEvent.StoreId);

        var store = await _context.Stores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == domainEvent.StoreId, cancellationToken);
        if (store == null)
        {
            _logger.LogWarning("Store with ID {StoreId} not found.", domainEvent.StoreId);
            return;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == store.OwnerId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User associated with Store {StoreId} not found.", domainEvent.StoreId);
            return;
        }

        user.Update(user.FullName, UserRole.Customer);
        _logger.LogInformation("User {UserId} demoted to Customer due to subscription expiry.", user.Id);

        store.Deactivate();
        _logger.LogInformation("Store {StoreId} deactivated.", store.Id);

        await _context.SaveChangesAsync(cancellationToken);

        await _emailService.SendSubscriptionExpiredEmailAsync(user.Email, cancellationToken);
    }
}