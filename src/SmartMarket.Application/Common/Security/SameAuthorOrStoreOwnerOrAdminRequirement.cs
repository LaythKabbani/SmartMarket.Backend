using Microsoft.AspNetCore.Authorization;

namespace SmartMarket.Application.Common.Security;

public class SameAuthorOrStoreOwnerOrAdminRequirement : IAuthorizationRequirement { }