using FluentValidation;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProductStock;

public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
{
    public UpdateProductStockCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.QuantityChange)
            .NotEqual(0).WithMessage("Quantity change cannot be zero.");
    }
}