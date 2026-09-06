using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Commands.CreateStore;
using SmartMarket.Application.Features.Stores.Commands.DeleteStore;
using SmartMarket.Application.Features.Stores.Commands.UpdateStore;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Application.Features.Stores.Queries.GetStoreById;
using SmartMarket.Application.Features.Stores.Queries.GetStoresList;

namespace SmartMarket.WebApi.Controllers;

public class StoresController : ApiControllerBase
{
    // GET: api/stores?pageNumber=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PaginatedList<StoreDto>>> GetStores([FromQuery] GetStoresListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/stores/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<StoreDto>>> GetStoreById(Guid id)
    {
        var result = await Mediator.Send(new GetStoreByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/stores
    [HttpPost]
    public async Task<ActionResult<Result<Guid>>> CreateStore([FromBody] CreateStoreCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/stores/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> UpdateStore(Guid id, [FromBody] UpdateStoreCommand command)
    {
        if (id != command.Id)
            return BadRequest(Result<bool>.Failure("Route ID does not match Command ID."));

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/stores/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> DeleteStore(Guid id)
    {
        var result = await Mediator.Send(new DeleteStoreCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}