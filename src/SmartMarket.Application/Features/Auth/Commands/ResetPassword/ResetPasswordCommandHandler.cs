using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempt failed. Email not found: {Email}", request.Email);
            return Result<bool>.Failure("Invalid request.");
        }

        if (string.IsNullOrEmpty(user.PasswordResetOtp) ||
            user.PasswordResetOtp != request.Otp ||
            user.OtpExpiresAt == null ||
            user.OtpExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset attempt failed. Invalid or expired OTP for UserId: {UserId}, Email: {Email}", user.Id, user.Email);
            return Result<bool>.Failure("Invalid or expired OTP code.");
        }

        user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));

        user.SetPasswordResetOtp(null, null);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return Result<bool>.Success(true);
    }
}