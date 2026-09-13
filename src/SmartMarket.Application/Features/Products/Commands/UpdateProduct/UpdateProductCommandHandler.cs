using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            return Result<bool>.Failure("Unauthorized access.");
        }

        var product = await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return Result<bool>.Failure("Product not found.");
        }

        if (product.Store == null)
        {
            return Result<bool>.Failure("The store associated with this product was not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            product.Store,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<bool>.Failure("You are not authorized to update this product.");
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return Result<bool>.Failure("The specified category does not exist.");
        }

        product.Update(request.Name, request.Description, request.Price, request.StockQuantity, request.CategoryId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}