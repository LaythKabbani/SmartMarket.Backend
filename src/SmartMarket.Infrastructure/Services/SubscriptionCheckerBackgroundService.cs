using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Events;

namespace SmartMarket.Infrastructure.Services;

public class SubscriptionCheckerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionCheckerBackgroundService> _logger;

    public SubscriptionCheckerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionCheckerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Checker Service Started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking expired subscriptions.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckExpiredSubscriptionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var now = DateTime.UtcNow;

        var expiredSubscriptions = await context.MerchantSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        if (!expiredSubscriptions.Any()) return;

        _logger.LogInformation("Found {Count} expired subscriptions to process.", expiredSubscriptions.Count);

        foreach (var subscription in expiredSubscriptions)
        {
            await publisher.Publish(new DomainEventNotification<SubscriptionExpiredDomainEvent>(
                new SubscriptionExpiredDomainEvent(subscription.Id, subscription.StoreId)), cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}