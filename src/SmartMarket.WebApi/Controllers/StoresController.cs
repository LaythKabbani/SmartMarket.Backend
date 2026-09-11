using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Commands.CreateStore;
using SmartMarket.Application.Features.Stores.Commands.DeleteStore;
using SmartMarket.Application.Features.Stores.Commands.UpdateStore;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Application.Features.Stores.Queries.GetStoreById;
using SmartMarket.Application.Features.Stores.Queries.GetStoresList;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class StoresController : ApiControllerBase
{
    //GET: api/stores?pageNumber=1
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<StoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<StoreDto>>> GetStores([FromQuery] GetStoresListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/stores/{id}
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<StoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<StoreDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<StoreDto>>> GetStoreById(Guid id)
    {
        var result = await Mediator.Send(new GetStoreByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/stores
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> CreateStore([FromBody] CreateStoreRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var ownerId))
            return Unauthorized();

        var command = new CreateStoreCommand(request.Name, ownerId, request.Description, request.LogoUrl);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/stores/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateStore(Guid id, [FromBody] UpdateStoreRequest request)
    {
        var command = new UpdateStoreCommand(id, request.Name, request.Description, request.LogoUrl);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/stores/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> DeleteStore(Guid id)
    {
        var result = await Mediator.Send(new DeleteStoreCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}

public record CreateStoreRequest(string Name, string? Description, string? LogoUrl);
public record UpdateStoreRequest(string Name, string? Description, string? LogoUrl);