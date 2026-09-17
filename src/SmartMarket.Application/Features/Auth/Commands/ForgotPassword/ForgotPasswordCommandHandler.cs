using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existing email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }

        var otp = Random.Shared.Next(100000, 999999).ToString();

        user.SetPasswordResetOtp(otp, DateTime.UtcNow.AddMinutes(10));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset OTP generated and sent for user email: {Email}", user.Email);

        await _emailService.SendPasswordResetEmailAsync(user.Email, otp, cancellationToken);

        return Result<bool>.Success(true);
    }
}