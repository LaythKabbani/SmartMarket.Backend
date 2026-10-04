using FluentValidation;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("StoreId is Required and can't be empty.");

        RuleFor(x => x.PlanType)
            .IsInEnum()
            .WithMessage("Plan Type is not valid.");
    }
}