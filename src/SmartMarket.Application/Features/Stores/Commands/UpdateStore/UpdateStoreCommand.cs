using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Stores.Commands.UpdateStore;

public record UpdateStoreCommand(
    Guid Id,
    string Name,
    string? Description = null,
    string? LogoUrl = null
) : IRequest<Result<bool>>;