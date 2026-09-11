using Microsoft.AspNetCore.Authorization;
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

[Authorize]
public class ProductsController : ApiControllerBase
{
    // GET: api/products
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ProductDto>>> GetProducts([FromQuery] GetProductsListQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    // GET: api/products/{id}
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<ProductDto>>> GetProductById(Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/products
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/products/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId
        );

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PATCH: api/products/{id}/stock
    [HttpPatch("{id:guid}/stock")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> UpdateStock(Guid id, [FromBody] UpdateProductStockRequest request)
    {
        var result = await Mediator.Send(new UpdateProductStockCommand(id, request.QuantityChange));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> DeleteProduct(Guid id)
    {
        var result = await Mediator.Send(new DeleteProductCommand(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}

public record UpdateProductRequest(string Name, string Description, decimal Price, int StockQuantity, Guid CategoryId);
public record UpdateProductStockRequest(int QuantityChange);