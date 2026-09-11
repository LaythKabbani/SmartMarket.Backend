using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Carts.Commands.AddToCart;
using SmartMarket.Application.Features.Carts.Commands.RemoveFromCart;
using SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;
using SmartMarket.Application.Features.Carts.Dtos;
using SmartMarket.Application.Features.Carts.Queries.GetCartByUserId;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class CartsController : ApiControllerBase
{
    // GET: api/carts/my-cart
    [HttpGet("my-cart")]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<CartDto>>> GetMyCart()
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var result = await Mediator.Send(new GetCartByUserIdQuery(userId));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/carts/add
    [HttpPost("add")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> AddToCart([FromBody] AddToCartRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var command = new AddToCartCommand(userId, request.ProductId, request.Quantity);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/carts/quantity
    [HttpPut("quantity")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateQuantity([FromBody] UpdateCartItemQuantityRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var command = new UpdateCartItemQuantityCommand(userId, request.ProductId, request.Quantity);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/carts/items/{productId}
    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> RemoveItem(Guid productId)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var result = await Mediator.Send(new RemoveFromCartCommand(userId, productId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record AddToCartRequest(Guid ProductId, int Quantity);
public record UpdateCartItemQuantityRequest(Guid ProductId, int Quantity);