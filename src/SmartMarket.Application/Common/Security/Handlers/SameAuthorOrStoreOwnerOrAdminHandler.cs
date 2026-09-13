using Microsoft.AspNetCore.Authorization;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using System.Security.Claims;

namespace SmartMarket.Application.Common.Security.Handlers;

public class SameAuthorOrStoreOwnerOrAdminHandler : AuthorizationHandler<SameAuthorOrStoreOwnerOrAdminRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameAuthorOrStoreOwnerOrAdminRequirement requirement,
        Order resource)
    {
        if (context.User == null || resource == null)
            return Task.CompletedTask;

        if (context.User.IsInRole(UserRole.SuperAdmin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
            return Task.CompletedTask;

        if (resource.UserId == currentUserId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        bool isMerchantOwner = resource.Items != null && resource.Items
            .Any(i => i.Product != null && i.Product.Store != null && i.Product.Store.OwnerId == currentUserId);

        if (isMerchantOwner)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}