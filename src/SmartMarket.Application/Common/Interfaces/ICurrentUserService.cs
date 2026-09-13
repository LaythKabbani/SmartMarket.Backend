using System.Security.Claims;

namespace SmartMarket.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
    ClaimsPrincipal? User { get; }
}