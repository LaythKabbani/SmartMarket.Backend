using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Auth.Commands.Login;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Settings;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests : TestBase
{
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
    private readonly JwtSettings _jwtSettings;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        _jwtSettings = new JwtSettings
        {
            RefreshTokenExpiryDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);

        _handler = new LoginCommandHandler(
            Context,
            _jwtTokenGeneratorMock.Object,
            _passwordHasherMock.Object,
            jwtOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // 1. Arrange: Send login request with an email not in DB
        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Incorrect Email or Password.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPasswordIsInvalid()
    {
        // 1. Arrange: Create user, but configure password hasher to return false
        var user = new User("Ahmad", "user@example.com", "HashedPassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("WrongPassword", user.PasswordHash))
            .Returns(false);

        var command = new LoginCommand("user@example.com", "WrongPassword");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Incorrect Email or Password.");
    }

    [Fact]
    public async Task Handle_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // 1. Arrange: Create user in DB and setup mocks for successful validation & token generation
        var user = new User("Ahmad", "valid@example.com", "HashedPassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("CorrectPassword", user.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns("fake-access-token");

        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        var command = new LoginCommand("valid@example.com", "CorrectPassword");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("fake-access-token");
        result.Data!.RefreshToken.Should().Be("fake-refresh-token");
        result.Data!.Email.Should().Be("valid@example.com");

        // Database Verification: Verify Refresh Token was saved correctly in DB
        var savedRefreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "fake-refresh-token");

        savedRefreshToken.Should().NotBeNull();
        savedRefreshToken!.UserId.Should().Be(user.Id);
        savedRefreshToken!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}