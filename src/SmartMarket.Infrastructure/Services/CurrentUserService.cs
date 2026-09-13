using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartMarket.Application.Common.Interfaces;

namespace SmartMarket.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}