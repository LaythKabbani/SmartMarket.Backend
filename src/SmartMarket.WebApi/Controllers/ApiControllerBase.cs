using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SmartMarket.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);

    protected string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    protected bool IsSuperAdmin => User.IsInRole("SuperAdmin");
    protected bool IsMerchant => User.IsInRole("Merchant");
    protected bool IsCustomer => User.IsInRole("Customer");
}