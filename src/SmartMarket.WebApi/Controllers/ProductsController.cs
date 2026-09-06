using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Products.Commands.CreateProduct;
using SmartMarket.Application.Features.Products.Commands.DeleteProduct;
using SmartMarket.Application.Features.Products.Commands.UpdateProduct;
using SmartMarket.Application.Features.Products.Commands.UpdateProductStock;
using SmartMarket.Application.Features.Products.Dtos;
using SmartMarket.Application.Features.Products.Queries.GetProductById;
using SmartMarket.Application.Features.Products.Queries.GetProductsList;

namespace SmartMarket.WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<PaginatedList<ProductDto>>> GetProducts([FromQuery] GetProductsListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/products/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<ProductDto>>> GetProductById(Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Result<Guid>>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/products/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest(Result<bool>.Failure("Route ID does not match Command ID."));

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PATCH: api/products/{id}/stock
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<Result<bool>>> UpdateStock(Guid id, [FromBody] int quantityChange)
    {
        var result = await Mediator.Send(new UpdateProductStockCommand(id, quantityChange));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<bool>>> DeleteProduct(Guid id)
    {
        var result = await Mediator.Send(new DeleteProductCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}