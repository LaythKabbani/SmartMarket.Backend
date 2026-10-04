using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ICurrentUserService currentUserService,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated attempt to create a subscription for store {StoreId}", request.StoreId);
            return Result<Guid>.Failure("Unauthorized access. User is not authenticated.");
        }

        var existingActiveSubscription = await _context.MerchantSubscriptions
            .AnyAsync(s => s.StoreId == request.StoreId && s.Status == SubscriptionStatus.Active && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (existingActiveSubscription)
        {
            _logger.LogWarning("User {UserId} attempted to create a duplicate active subscription for store {StoreId}", userId, request.StoreId);
            return Result<Guid>.Failure("This store already has an active subscription.");
        }

        if (request.PlanType != SubscriptionPlan.Demo)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentToken))
            {
                return Result<Guid>.Failure("Payment token is required for paid subscription plans.");
            }

            var paymentVerification = await _paymentService.ValidateTokenAsync(request.PaymentToken, cancellationToken);
            if (!paymentVerification.IsSuccess)
            {
                _logger.LogWarning("Invalid payment token {Token} for store {StoreId}", request.PaymentToken, request.StoreId);
                return Result<Guid>.Failure(paymentVerification.ErrorMessage ?? "Invalid or expired payment token.");
            }
        }

        int durationDays = request.PlanType switch
        {
            SubscriptionPlan.Monthly => 30,
            SubscriptionPlan.Yearly => 365,
            SubscriptionPlan.Demo => 14,
            _ => 30
        };

        var subscription = new MerchantSubscription
        (
            request.StoreId,
            request.PlanType,
            subscribedAt: DateTime.UtcNow,
            expiresAt: DateTime.UtcNow.AddDays(durationDays),
            status: SubscriptionStatus.Active
        );

        _context.MerchantSubscriptions.Add(subscription);

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found during subscription creation for store {StoreId}", userId, request.StoreId);
            return Result<Guid>.Failure("User not found.");
        }

        user.Update(user.FullName, UserRole.Merchant);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription {SubscriptionId} created successfully for store {StoreId} by user {UserId}", subscription.Id, request.StoreId, userId);

        return Result<Guid>.Success(subscription.Id);
    }
}