using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using SmartMarket.Domain.Settings;

namespace SmartMarket.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtOption,
        ILogger<RegisterCommandHandler> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtOption.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed. Email already exists: {Email}", request.Email);
            return Result<AuthResponse>.Failure("Email already Exists.");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = new User(
            request.FullName,
            request.Email,
            hashedPassword,
            request.Role
        );

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new SmartMarket.Domain.Entities.RefreshToken(
            refreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            user.Id
        );

        user.RefreshTokens.Add(refreshToken);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}, Role: {Role}", user.Id, user.Email, user.Role);

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