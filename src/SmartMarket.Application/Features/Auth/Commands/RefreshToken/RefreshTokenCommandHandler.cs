using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Settings;

namespace SmartMarket.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtOptions,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Failed token refresh attempt. Reason: Refresh token does not belong to any user.");
            return Result<AuthResponse>.Failure("Invalid refresh token.");
        }

        var existingToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            _logger.LogWarning("Failed token refresh attempt for UserId: {UserId}. Reason: Token is expired or revoked.", user.Id);
            return Result<AuthResponse>.Failure("Refresh token has expired or been revoked.");
        }

        existingToken.Revoke();

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = new Domain.Entities.RefreshToken(
            newRefreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            user.Id
        );

        user.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tokens refreshed successfully for UserId: {UserId}", user.Id);

        var response = new AuthResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            newAccessToken,
            newRefreshTokenString
        );

        return Result<AuthResponse>.Success(response);
    }
}