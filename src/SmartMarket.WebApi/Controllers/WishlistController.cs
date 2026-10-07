using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Wishlist.Commands.AddToWishlist;
using SmartMarket.Application.Features.Wishlist.Commands.RemoveFromWishlist;
using SmartMarket.Application.Features.Wishlist.Dtos;
using SmartMarket.Application.Features.Wishlist.Queries.GetWishlist;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class WishlistController : ApiControllerBase
{
    [HttpGet("my-wishlist")]
    [ProducesResponseType(typeof(Result<List<WishlistItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<List<WishlistItemDto>>>> GetMyWishlist()
    {
        if (!Guid.TryParse(CurrentUserId, out var userId)) return Unauthorized();
        var result = await Mediator.Send(new GetWishlistQuery(userId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("add")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<bool>>> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId)) return Unauthorized();
        var result = await Mediator.Send(new AddToWishlistCommand(userId, request.ProductId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<bool>>> RemoveFromWishlist(Guid productId)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId)) return Unauthorized();
        var result = await Mediator.Send(new RemoveFromWishlistCommand(userId, productId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record AddToWishlistRequest(Guid ProductId);
