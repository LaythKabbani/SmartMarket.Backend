using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Infrastructure.Security.Handlers;

public class CanCreateStoreHandler : AuthorizationHandler<CanCreateStoreRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanCreateStoreRequirement requirement)
    {
        var isAuthorized = context.User.IsInRole(UserRole.Merchant.ToString()) ||
                           context.User.IsInRole(UserRole.SuperAdmin.ToString());

        if (isAuthorized)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}