using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProductStock;

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return Result<bool>.Failure("Product not found.");
        }

        if (product.StockQuantity + request.QuantityChange < 0)
        {
            return Result<bool>.Failure("Insufficient stock quantity.");
        }

        product.UpdateStock(request.QuantityChange);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}