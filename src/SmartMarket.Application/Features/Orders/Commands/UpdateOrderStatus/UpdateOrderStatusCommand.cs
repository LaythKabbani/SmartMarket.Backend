using MediatR;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus Status
) : IRequest<Result<bool>>;