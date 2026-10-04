using FluentValidation;

namespace SmartMarket.Application.Features.Subscriptions.Commands.UpdatePlanPrice;

public class UpdatePlanPriceCommandValidator : AbstractValidator<UpdatePlanPriceCommand>
{
    public UpdatePlanPriceCommandValidator()
    {
        RuleFor(v => v.PlanType)
            .IsInEnum()
            .WithMessage("Invalid subscription plan type.");

        RuleFor(v => v.NewPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("New price must be greater than or equal to zero.");
    }
}