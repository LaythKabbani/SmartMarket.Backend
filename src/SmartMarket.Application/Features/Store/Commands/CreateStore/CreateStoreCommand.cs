using FluentValidation;
using MediatR;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Stores.Commands.CreateStore;

public record CreateStoreCommand(
    string Name,
    Guid OwnerId,
    string? Description = null,
    string? LogoUrl = null
) : IRequest<Result<Guid>>;