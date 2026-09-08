using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Orders.Commands.CancelOrder;
using SmartMarket.Application.Features.Orders.Commands.CreateOrder;
using SmartMarket.Application.Features.Orders.Commands.UpdateOrderStatus;
using SmartMarket.Application.Features.Orders.Dtos;
using SmartMarket.Application.Features.Orders.Queries.GetOrderById;
using SmartMarket.Application.Features.Orders.Queries.GetUserOrders;
using SmartMarket.Domain.Enums;
using SmartMarket.WebApi.Controllers;

namespace SmartMarket.Api.Controllers;

public class OrdersController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<Guid>>> Create(CreateOrderCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<OrderDto>>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<Result<List<OrderDto>>>> GetByUserId(Guid userId)
    {
        var result = await Mediator.Send(new GetUserOrdersQuery(userId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Result<bool>>> UpdateStatus(Guid id, [FromBody] OrderStatus status)
    {
        var result = await Mediator.Send(new UpdateOrderStatusCommand(id, status));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<Result<bool>>> Cancel(Guid id)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}