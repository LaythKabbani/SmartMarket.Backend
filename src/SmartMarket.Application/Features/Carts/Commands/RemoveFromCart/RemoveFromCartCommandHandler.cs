using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Carts.Commands.RemoveFromCart;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public RemoveFromCartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
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

        cart.RemoveItem(request.ProductId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}