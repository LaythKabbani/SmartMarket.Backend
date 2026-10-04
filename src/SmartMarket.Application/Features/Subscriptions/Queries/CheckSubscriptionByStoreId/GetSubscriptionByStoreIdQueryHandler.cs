using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Dtos;

namespace SmartMarket.Application.Features.Subscriptions.Queries.GetSubscriptionByStoreId;

public class GetSubscriptionByStoreIdQueryHandler : IRequestHandler<GetSubscriptionByStoreIdQuery, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetSubscriptionByStoreIdQueryHandler> _logger;

    public GetSubscriptionByStoreIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetSubscriptionByStoreIdQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionByStoreIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated attempt to access subscription for store {StoreId}", request.StoreId);
            return Result<SubscriptionDto>.Failure("Unauthorized access. User is not authenticated.");
        }

        var subscription = await (
            from s in _context.MerchantSubscriptions.AsNoTracking()
            where s.StoreId == request.StoreId
            orderby s.SubscripedAt descending
            join plan in _context.SubscriptionPlans.AsNoTracking()
                on s.PlanType equals plan.PlanType
            select new SubscriptionDto
            {
                Id = s.Id,
                StoreId = s.StoreId,
                Plan = s.PlanType,
                Status = s.Status,
                SubscripedAt = s.SubscripedAt,
                ExpiresAt = s.ExpiresAt,
                Price = plan.Price
            }
            ).FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription record for store {StoreId} was not found", request.StoreId);
            return Result<SubscriptionDto>.Failure($"No subscription record found for Store ID '{request.StoreId}'.");
        }

        _logger.LogInformation("Successfully retrieved subscription for store {StoreId} by user {UserId}", request.StoreId, userId);

        return Result<SubscriptionDto>.Success(subscription);
    }
}