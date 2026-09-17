using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Settings;

namespace SmartMarket.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtOptions,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Failed login attempt. Reason: Email not found. Email: {Email}", request.Email);
            return Result<AuthResponse>.Failure("Incorrect Email or Password.");
        }

        var verificationResult = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!verificationResult)
        {
            _logger.LogWarning("Failed login attempt. Reason: Invalid password. UserId: {UserId}, Email: {Email}", user.Id, request.Email);
            return Result<AuthResponse>.Failure("Incorrect Email or Password.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new Domain.Entities.RefreshToken(
            refreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            user.Id
        );

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        var response = new AuthResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            accessToken,
            refreshTokenString
        );

        return Result<AuthResponse>.Success(response);
    }
}