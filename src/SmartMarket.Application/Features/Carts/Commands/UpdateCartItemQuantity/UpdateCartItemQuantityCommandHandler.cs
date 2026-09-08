using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateCartItemQuantityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
        {
            return Result<bool>.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null)
        {
            return Result<bool>.Failure("Product not found in cart.");
        }

        var product = await _context.Products.FindAsync(new object[] { request.ProductId }, cancellationToken);
        if (product == null || product.StockQuantity < request.NewQuantity)
        {
            return Result<bool>.Failure("Requested quantity exceeds available stock.");
        }

        item.UpdateQuantity(request.NewQuantity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}