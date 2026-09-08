using FluentValidation;

namespace SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewQuantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero. If you wish to remove the item, use Remove From Cart.");
    }
}