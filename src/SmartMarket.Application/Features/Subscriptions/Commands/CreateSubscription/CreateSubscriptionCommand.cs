using MediatR;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid StoreId,
    SubscriptionPlan PlanType,
    string? PaymentToken
) : IRequest<Result<Guid>>;