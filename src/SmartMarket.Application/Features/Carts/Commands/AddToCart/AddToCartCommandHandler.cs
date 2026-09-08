using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Carts.Commands.AddToCart;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AddToCartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
            return Result<Guid>.Failure("Product not found.");

        if (product.StockQuantity < request.Quantity)
            return Result<Guid>.Failure($"Insufficient stock. Available: {product.StockQuantity}");

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = new Cart(request.UserId);
            _context.Carts.Add(cart);
        }

        cart.AddOrUpdateItem(request.ProductId, request.Quantity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(cart.Id);
    }
}