using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid UserId,
    string ShippingAddress,
    List<OrderItemRequest> Items
) : IRequest<Result<Guid>>;