using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Categories.Commands.CreateCategory;
using SmartMarket.Application.Features.Categories.Commands.DeleteCategory;
using SmartMarket.Application.Features.Categories.Commands.UpdateCategory;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Application.Features.Categories.Queries.GetCategoriesList;
using SmartMarket.Application.Features.Categories.Queries.GetCategoryById;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class CategoriesController : ApiControllerBase
{
    // GET: api/categories
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        return Ok(await Mediator.Send(new GetCategoriesListQuery()));
    }

    // GET: api/categories/{id}
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<CategoryDto>>> GetCategoryById(Guid id)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/categories
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/categories/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Description);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/categories/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> DeleteCategory(Guid id)
    {
        var result = await Mediator.Send(new DeleteCategoryCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}

public record UpdateCategoryRequest(string Name, string? Description);