using MediatR;
using SmartMarket.Application.Common.Models;

public record UpdatePlanPriceCommand(SubscriptionPlan PlanType, decimal NewPrice) : IRequest<Result<bool>>;