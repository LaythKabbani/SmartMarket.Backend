using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string Description) : IRequest<Result<Guid>>;