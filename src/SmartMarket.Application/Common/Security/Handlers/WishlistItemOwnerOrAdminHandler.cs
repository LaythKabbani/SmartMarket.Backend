using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Infrastructure.Security.Handlers;

public class WishlistItemOwnerOrAdminHandler : AuthorizationHandler<OwnerOrSuperAdminRequirement, WishlistItem>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrSuperAdminRequirement requirement,
        WishlistItem resource)
    {
        if (resource == null) return Task.CompletedTask;

        if (context.User.IsInRole(UserRole.SuperAdmin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var currentUserId) && resource.UserId == currentUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
