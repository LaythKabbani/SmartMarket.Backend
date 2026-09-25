using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Auth.Commands.ForgotPassword;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests : TestBase
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _loggerMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ForgotPasswordCommandHandler>>();

        _handler = new ForgotPasswordCommandHandler(
            Context,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserNotFound()
    {
        // 1. Arrange: Send request with non-existent email
        var command = new ForgotPasswordCommand("nonexistent@example.com");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that Email Service was NEVER called for non-existing user
        _emailServiceMock.Verify(
            s => s.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateOtpAndSendEmail_WhenUserExists()
    {
        // 1. Arrange: Create a user in the database
        var user = new User("Ahmad", "existing@example.com", "HashedOldPassword");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var command = new ForgotPasswordCommand("existing@example.com");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();

        // Database Verification: Check OTP and Expiration date set correctly
        var updatedUser = await Context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "existing@example.com");

        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordResetOtp.Should().NotBeNullOrEmpty();
        updatedUser.PasswordResetOtp.Should().HaveLength(6);
        updatedUser.OtpExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verify that Email Service was called once with valid arguments
        _emailServiceMock.Verify(
            s => s.SendPasswordResetEmailAsync(
                "existing@example.com",
                updatedUser.PasswordResetOtp!,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenEmailServiceFails()
    {
        // 1. Arrange: User exists, but Email Service throws an Exception
        var user = new User("Ahmad", "error@example.com", "HashedOldPassword");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        _emailServiceMock
            .Setup(s => s.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP server is down"));

        var command = new ForgotPasswordCommand("error@example.com");

        // 2. Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("SMTP server is down");
    }
}