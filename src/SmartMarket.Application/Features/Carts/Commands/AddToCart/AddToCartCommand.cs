using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Carts.Commands.AddToCart;

public record AddToCartCommand(
    Guid UserId,
    Guid ProductId,
    int Quantity
) : IRequest<Result<Guid>>;