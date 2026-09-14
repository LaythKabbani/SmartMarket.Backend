using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;

namespace SmartMarket.Infrastructure.Services;

public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("==================================================");
        _logger.LogInformation("📬 [FAKE EMAIL SERVICE] Password Reset Request");
        _logger.LogInformation("To: {Email}", toEmail);
        _logger.LogInformation("Reset OTP: {otp}", otp);
        _logger.LogInformation("==================================================");

        return Task.CompletedTask;
    }
}