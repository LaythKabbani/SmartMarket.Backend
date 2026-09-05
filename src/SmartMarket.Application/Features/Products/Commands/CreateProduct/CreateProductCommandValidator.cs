using FluentValidation;

namespace SmartMarket.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(p => p.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(p => p.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(p => p.StoreId)
            .NotEmpty().WithMessage("Store ID is required.");

        RuleFor(p => p.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}