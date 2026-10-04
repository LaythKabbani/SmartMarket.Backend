using FluentValidation;

namespace SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;

public class RenewSubscriptionCommandValidator : AbstractValidator<RenewSubscriptionCommand>
{
    public RenewSubscriptionCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("Store ID is required.");

        RuleFor(x => x.PlanType)
            .IsInEnum()
            .WithMessage("Invalid subscription plan type.");
    }
}