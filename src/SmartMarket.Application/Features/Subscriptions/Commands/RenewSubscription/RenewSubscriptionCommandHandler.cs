using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;

public class RenewSubscriptionCommandHandler : IRequestHandler<RenewSubscriptionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<RenewSubscriptionCommandHandler> _logger;

    public RenewSubscriptionCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ILogger<RenewSubscriptionCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated attempt to renew subscription for store {StoreId}", request.StoreId);
            return Result<bool>.Failure("Unauthorized access. User is not authenticated.");
        }

        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StoreId, cancellationToken);

        if (store == null)
        {
            _logger.LogWarning("Store with ID {StoreId} not found during subscription renewal", request.StoreId);
            return Result<bool>.Failure($"Store with ID '{request.StoreId}' was not found.");
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User!,
            store,
            new OwnerOrSuperAdminRequirement());

        if (!authorizationResult.Succeeded)
        {
            _logger.LogWarning("User {UserId} attempted to renew subscription for store {StoreId} without ownership/admin permission", userId, request.StoreId);
            return Result<bool>.Failure("Forbidden: You do not have permission to renew subscription for this store.");
        }

        var subscription = await _context.MerchantSubscriptions
            .OrderByDescending(s => s.SubscripedAt)
            .FirstOrDefaultAsync(s => s.StoreId == request.StoreId, cancellationToken);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription for store {StoreId} not found for renewal", request.StoreId);
            return Result<bool>.Failure($"No subscription found for Store ID '{request.StoreId}'.");
        }

        if (request.PlanType != SubscriptionPlan.Demo)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentToken))
            {
                return Result<bool>.Failure("Payment token is required for renewing paid subscriptions.");
            }

            var paymentVerification = await _paymentService.ValidateTokenAsync(request.PaymentToken, cancellationToken);
            if (!paymentVerification.IsSuccess)
            {
                _logger.LogWarning("Invalid payment token {Token} during renewal for store {StoreId}", request.PaymentToken, request.StoreId);
                return Result<bool>.Failure(paymentVerification.ErrorMessage ?? "Invalid or expired payment token.");
            }
        }

        int durationDays = request.PlanType switch
        {
            SubscriptionPlan.Monthly => 30,
            SubscriptionPlan.ThreeMonths => 90,
            SubscriptionPlan.SixMonths => 180,
            SubscriptionPlan.Yearly => 365,
            SubscriptionPlan.Demo => 14,
            _ => 30
        };

        var baseDate = subscription.IsActive ? subscription.ExpiresAt : DateTime.UtcNow;

        subscription.ExpiresAt = baseDate.AddDays(durationDays);
        subscription.PlanType = request.PlanType;
        subscription.Status = SubscriptionStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription {SubscriptionId} successfully renewed until {ExpiresAt} for store {StoreId}",
            subscription.Id, subscription.ExpiresAt, request.StoreId);

        return Result<bool>.Success(true);
    }
}