using MediatR;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartMarket.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var validationInfo = await _context.Stores
            .Where(s => s.Id == request.StoreId)
            .Select(s => new {
                StoreExists = true,
                CategoryExists = _context.Categories.Any(c => c.Id == request.CategoryId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (validationInfo == null)
            return Result<Guid>.Failure("The specified store does not exist.");

        if (!validationInfo.CategoryExists)
            return Result<Guid>.Failure("The specified category does not exist.");

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