using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Orders.Commands.CancelOrder;
using SmartMarket.Application.Features.Orders.Commands.CreateOrder;
using SmartMarket.Application.Features.Orders.Commands.UpdateOrderStatus;
using SmartMarket.Application.Features.Orders.Dtos;
using SmartMarket.Application.Features.Orders.Queries.GetOrderById;
using SmartMarket.Application.Features.Orders.Queries.GetUserOrders;
using SmartMarket.Domain.Enums;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class OrdersController : ApiControllerBase
{
    // POST: api/orders
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> Create([FromBody] CreateOrderRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var command = new CreateOrderCommand(userId, request.ShippingAddress);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // GET: api/orders/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<OrderDto>>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // GET: api/orders/my-orders
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(Result<List<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<List<OrderDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<List<OrderDto>>>> GetMyOrders()
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var result = await Mediator.Send(new GetUserOrdersQuery(userId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PATCH: api/orders/{id}/status
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await Mediator.Send(new UpdateOrderStatusCommand(id, request.Status));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/orders/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> Cancel(Guid id)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record CreateOrderRequest(string ShippingAddress);
public record UpdateOrderStatusRequest(OrderStatus Status);