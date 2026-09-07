using FluentValidation;
using MediatR;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(150).WithMessage("Store name must not exceed 150 characters.");

        RuleFor(s => s.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.");

        RuleFor(s => s.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}