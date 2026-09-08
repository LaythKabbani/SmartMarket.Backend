using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(o => o.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(o => o.ShippingAddress).NotEmpty().WithMessage("Shipping address is required.");
        RuleFor(o => o.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(o => o.Items)
            .SetValidator(new OrderItemRequestValidator());
    }
}