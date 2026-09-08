using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Carts.Commands.AddToCart;
using SmartMarket.Application.Features.Carts.Commands.RemoveFromCart;
using SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;
using SmartMarket.Application.Features.Carts.Dtos;
using SmartMarket.Application.Features.Carts.Queries.GetCartByUserId;

namespace SmartMarket.WebApi.Controllers;

public class CartsController : ApiControllerBase
{
    // GET: api/carts/user/{userId}
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<Result<CartDto>>> GetByUserId(Guid userId)
    {
        var result = await Mediator.Send(new GetCartByUserIdQuery(userId));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/carts/add
    [HttpPost("add")]
    public async Task<ActionResult<Result<Guid>>> AddToCart([FromBody] AddToCartCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/carts/quantity
    [HttpPut("quantity")]
    public async Task<ActionResult<Result<bool>>> UpdateQuantity([FromBody] UpdateCartItemQuantityCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/carts/user/{userId}/items/{productId}
    [HttpDelete("user/{userId:guid}/items/{productId:guid}")]
    public async Task<ActionResult<Result<bool>>> RemoveItem(Guid userId, Guid productId)
    {
        var result = await Mediator.Send(new RemoveFromCartCommand(userId, productId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}