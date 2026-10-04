using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated attempt to cancel subscription for store {StoreId}", request.StoreId);
            return Result<bool>.Failure("Unauthorized access. User is not authenticated.");
        }

        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StoreId, cancellationToken);

        if (store == null)
        {
            _logger.LogWarning("Store with ID {StoreId} not found during cancellation", request.StoreId);
            return Result<bool>.Failure($"Store with ID '{request.StoreId}' was not found.");
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User!,
            store,
            new OwnerOrSuperAdminRequirement());

        if (!authorizationResult.Succeeded)
        {
            _logger.LogWarning("User {UserId} attempted to cancel subscription for store {StoreId} without ownership/admin permission", userId, request.StoreId);
            return Result<bool>.Failure("Forbidden: You do not have permission to modify subscriptions for this store.");
        }

        var subscription = await _context.MerchantSubscriptions
            .OrderByDescending(s => s.SubscripedAt)
            .FirstOrDefaultAsync(s => s.StoreId == request.StoreId && s.Status == SubscriptionStatus.Active, cancellationToken);

        if (subscription == null)
        {
            _logger.LogWarning("Active subscription for store {StoreId} was not found for cancellation", request.StoreId);
            return Result<bool>.Failure($"No active subscription found for Store ID '{request.StoreId}'.");
        }

        subscription.Status = SubscriptionStatus.Cancelled;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription {SubscriptionId} for store {StoreId} was cancelled by user {UserId}. Reason: {Reason}",
            subscription.Id, request.StoreId, userId, request.CancellationReason ?? "No reason provided");

        return Result<bool>.Success(true);
    }
}