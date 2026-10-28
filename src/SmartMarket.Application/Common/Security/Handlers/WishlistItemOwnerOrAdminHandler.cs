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
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Task.CompletedTask;
        }

        var isSuperAdmin = context.User.IsInRole(UserRole.SuperAdmin.ToString());
        var isOwner = resource.UserId == currentUserId;

        if (isOwner || isSuperAdmin)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
