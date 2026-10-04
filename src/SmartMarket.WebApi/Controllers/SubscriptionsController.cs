using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.CancelSubscription;
using SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;
using SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;
using SmartMarket.Application.Features.Subscriptions.Dtos;
using SmartMarket.Application.Features.Subscriptions.Queries.GetSubscriptionByStoreId;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class SubscriptionsController : ApiControllerBase
{
    // GET: api/subscriptions/{storeId}
    [HttpGet("{storeId:guid}")]
    [ProducesResponseType(typeof(Result<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubscriptionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<SubscriptionDto>>> CheckSubscriptionByStoreId(Guid storeId)
    {
        var result = await Mediator.Send(new GetSubscriptionByStoreIdQuery(storeId));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/subscriptions
    [HttpPost]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<Guid>>> CreateSubscription([FromBody] CreateSubscriptionCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/subscriptions/renew
    [HttpPost("renew")]
    [Authorize(Roles = "SuperAdmin,Merchant")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> RenewSubscription([FromBody] RenewSubscriptionCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/subscriptions/cancel
    [HttpPost("cancel")]
    [Authorize(Roles = "SuperAdmin,Merchant")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> CancelSubscription([FromBody] CancelSubscriptionCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/subscriptions/plans/price
    [HttpPut("plans/price")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<bool>>> UpdatePlanPrice([FromBody] UpdatePlanPriceCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}