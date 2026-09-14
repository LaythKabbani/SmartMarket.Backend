namespace SmartMarket.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
}