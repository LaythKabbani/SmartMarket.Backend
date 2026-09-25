using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(string Name, string Description, decimal Price, int StockQuantity, Guid StoreId, Guid CategoryId)
    : IRequest<Result<Guid>>;