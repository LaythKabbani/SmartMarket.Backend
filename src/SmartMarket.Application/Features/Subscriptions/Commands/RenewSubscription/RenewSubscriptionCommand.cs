using MediatR;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;

public record RenewSubscriptionCommand(
    Guid StoreId,
    SubscriptionPlan PlanType,
    string? PaymentToken
) : IRequest<Result<bool>>;