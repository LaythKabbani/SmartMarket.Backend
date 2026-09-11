using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Users.Commands.CreateUser;
using SmartMarket.Application.Features.Users.Commands.DeleteUser;
using SmartMarket.Application.Features.Users.Commands.UpdateUser;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Application.Features.Users.Queries.GetUserById;
using SmartMarket.Application.Features.Users.Queries.GetUsersList;
using SmartMarket.Domain.Enums;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class UsersController : ApiControllerBase
{
    // GET: api/users/me
    [HttpGet("me")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<UserDto>>> GetCurrentUser()
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var result = await Mediator.Send(new GetUserByIdQuery(userId));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // GET: api/users?pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedList<UserDto>>> GetUsers([FromQuery] GetUsersListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/users/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<UserDto>>> GetUserById(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/users
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/users/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var command = new UpdateUserCommand(id, request.Fullname, request.Role);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> DeleteUser(Guid id)
    {
        var result = await Mediator.Send(new DeleteUserCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}

public record UpdateUserRequest(string Fullname, UserRole Role);