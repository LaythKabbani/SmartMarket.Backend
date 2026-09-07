using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Users.Commands.CreateUser;
using SmartMarket.Application.Features.Users.Commands.DeleteUser;
using SmartMarket.Application.Features.Users.Commands.UpdateUser;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Application.Features.Users.Queries.GetUserById;
using SmartMarket.Application.Features.Users.Queries.GetUsersList;

namespace SmartMarket.WebApi.Controllers;

public class UsersController : ApiControllerBase
{
    // GET: api/users?pageNumber=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PaginatedList<UserDto>>> GetUsers([FromQuery] GetUsersListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<UserDto>>> GetUserById(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<Result<Guid>>> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/users/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id)
            return BadRequest(Result<bool>.Failure("Route ID does not match Command ID."));

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> DeleteUser(Guid id)
    {
        var result = await Mediator.Send(new DeleteUserCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}