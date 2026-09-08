using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Carts.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    Guid UserId,
    Guid ProductId
) : IRequest<Result<bool>>;