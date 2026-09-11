using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, int StockQuantity, Guid CategoryId)
    : IRequest<Result<bool>>;