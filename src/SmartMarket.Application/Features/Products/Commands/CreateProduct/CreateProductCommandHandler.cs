using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public CreateProductCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            return Result<Guid>.Failure("Unauthorized access.");
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == request.StoreId, cancellationToken);

        if (store == null)
        {
            return Result<Guid>.Failure("The specified store does not exist.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            store,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<Guid>.Failure("You are not authorized to add products to this store.");
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return Result<Guid>.Failure("The specified category does not exist.");
        }

        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.StoreId,
            request.CategoryId
        );

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}