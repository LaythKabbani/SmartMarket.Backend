using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    Guid UserId,
    Guid ProductId,
    int NewQuantity
) : IRequest<Result<bool>>;