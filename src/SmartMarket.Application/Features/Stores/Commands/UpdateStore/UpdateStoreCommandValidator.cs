using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(s => s.Id)
            .NotEmpty().WithMessage("Store ID is required.");

        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(150).WithMessage("Store name must not exceed 150 characters.");

        RuleFor(s => s.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}