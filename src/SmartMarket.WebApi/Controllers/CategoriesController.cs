using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Categories.Commands.CreateCategory;
using SmartMarket.Application.Features.Categories.Commands.DeleteCategory;
using SmartMarket.Application.Features.Categories.Commands.UpdateCategory;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Application.Features.Categories.Queries.GetCategoriesList;
using SmartMarket.Application.Features.Categories.Queries.GetCategoryById;

namespace SmartMarket.WebApi.Controllers;

public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        return Ok(await Mediator.Send(new GetCategoriesListQuery()));
    }

    // GET: api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<CategoryDto>>> GetCategoryById(Guid id)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<Guid>>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
            return BadRequest(Result<bool>.Failure("Route ID does not match Command ID."));

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/categories/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> DeleteCategory(Guid id)
    {
        var result = await Mediator.Send(new DeleteCategoryCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}