using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces; // افترضنا وجود IApplicationDbContext أو IAuthService
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken
                                    && rt.UserId == request.UserId, cancellationToken);

        if (refreshToken is null)
        {
            return Result<bool>.Failure("Invalid refresh token or token does not belong to the user.");
        }

        if (refreshToken.IsRevoked)
        {
            return Result<bool>.Failure("Token has already been revoked.");
        }

        refreshToken.Revoke();

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}