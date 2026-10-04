using FluentValidation;

namespace SmartMarket.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("Store ID is required.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}