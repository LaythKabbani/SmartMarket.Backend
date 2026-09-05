using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public Guid StoreId { get; init; }
    public Guid CategoryId { get; init; }
}