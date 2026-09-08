using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId) : IRequest<Result<bool>>;