using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}