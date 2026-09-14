using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Invalid request.");
        }

        if (string.IsNullOrEmpty(user.PasswordResetOtp) ||
            user.PasswordResetOtp != request.Otp ||
            user.OtpExpiresAt == null ||
            user.OtpExpiresAt < DateTime.UtcNow)
        {
            return Result<bool>.Failure("Invalid or expired OTP code.");
        }

        user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));

        user.SetPasswordResetOtp(null, null);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}