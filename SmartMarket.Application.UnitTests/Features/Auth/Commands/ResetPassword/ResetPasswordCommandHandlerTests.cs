using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Auth.Commands.ResetPassword;
using SmartMarket.Domain.Entities;
using SmartMarket.Infrastructure.Persistence;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _passwordHasherMock
            .Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("HashedNewPassword123!");

        _handler = new ResetPasswordCommandHandler(_context, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // 1. Arrange: Sending a command with an email that does not exist in the database
        var command = new ResetPasswordCommand("nonexistent@example.com", "123456", "NewPassword123!");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOtpIsInvalid()
    {
        // 1. Arrange: Create a user and set an OTP code ("111111") using domain methods
        var user = new User("Ahmed", "user@example.com", "OldHashedPassword");
        user.SetPasswordResetOtp("111111", DateTime.UtcNow.AddMinutes(10));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Send request with an invalid OTP ("999999")
        var command = new ResetPasswordCommand("user@example.com", "999999", "NewPassword123!");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOtpIsExpired()
    {
        // 1. Arrange: Create a user with a valid OTP code but an expired timestamp
        var user = new User("Ahmed", "expired@example.com", "OldHashedPassword");
        user.SetPasswordResetOtp("123456", DateTime.UtcNow.AddMinutes(-10)); // Expired 10 minutes ago

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ResetPasswordCommand("expired@example.com", "123456", "NewPassword123!");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldResetPassword_WhenDataIsValid()
    {
        // 1. Arrange: Create a user with a valid OTP code and a future expiration timestamp
        var user = new User("Ahmed", "valid@example.com", "OldHashedPassword");
        user.SetPasswordResetOtp("123456", DateTime.UtcNow.AddMinutes(10));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ResetPasswordCommand("valid@example.com", "123456", "NewPassword123!");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();

        // Database Verification: Read clean state using AsNoTracking
        var updatedUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "valid@example.com");

        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordResetOtp.Should().BeNull();
        updatedUser.OtpExpiresAt.Should().BeNull();
        updatedUser.PasswordHash.Should().Be("HashedNewPassword123!");
    }
}