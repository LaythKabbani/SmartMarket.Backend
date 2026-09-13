using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using System.Security.Claims;

namespace SmartMarket.Infrastructure.Security.Handlers;

public class OrderOwnerOrAdminHandler : AuthorizationHandler<OwnerOrSuperAdminRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrSuperAdminRequirement requirement,
        Order resource)
    {
        if (context.User == null || resource == null)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(UserRole.SuperAdmin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Task.CompletedTask;
        }

        bool isStoreOwner = resource.Items != null && resource.Items
            .Any(i => i.Product != null && i.Product.Store != null && i.Product.Store.OwnerId == currentUserId);

        if (isStoreOwner)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}