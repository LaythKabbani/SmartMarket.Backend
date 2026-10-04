using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CancelSubscription;

public record CancelSubscriptionCommand(
    Guid StoreId,
    string? CancellationReason = null
) : IRequest<Result<bool>>;