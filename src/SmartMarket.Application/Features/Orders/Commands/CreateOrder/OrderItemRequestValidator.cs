using FluentValidation;

namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(i => i.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(i => i.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}