using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public Guid CategoryId { get; init; }
}