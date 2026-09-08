using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Products.Commands.UpdateProductStock;

public record UpdateProductStockCommand(Guid Id, int Quantity) : IRequest<Result<bool>>;