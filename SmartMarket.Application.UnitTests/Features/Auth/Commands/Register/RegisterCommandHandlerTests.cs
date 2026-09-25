using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Auth.Commands.Register;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using SmartMarket.Domain.Settings;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests : TestBase
{
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock;
    private readonly JwtSettings _jwtSettings;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

        _jwtSettings = new JwtSettings
        {
            RefreshTokenExpiryDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);

        _handler = new RegisterCommandHandler(
            Context,
            _jwtTokenGeneratorMock.Object,
            _passwordHasherMock.Object,
            jwtOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        // 1. Arrange: Add an existing user with the same email to the DB
        var existingUser = new User("Existing User", "duplicate@example.com", "HashedPassword123");
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var command = new RegisterCommand(
            "New User",
            "duplicate@example.com",
            "Password123!",
            UserRole.Customer);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already Exists.");
    }

    [Fact]
    public async Task Handle_ShouldRegisterUserAndReturnAuthResponse_WhenDataIsValid()
    {
        // 1. Arrange: Setup password hasher and token generator mocks
        _passwordHasherMock
            .Setup(h => h.HashPassword("Password123!"))
            .Returns("HashedPassword123!");

        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns("fake-access-token");

        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        var command = new RegisterCommand(
            "Ahmad Al-Mowafak",
            "newuser@example.com",
            "Password123!",
            UserRole.Customer);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FullName.Should().Be("Ahmad Al-Mowafak");
        result.Data!.Email.Should().Be("newuser@example.com");
        result.Data!.AccessToken.Should().Be("fake-access-token");
        result.Data!.RefreshToken.Should().Be("fake-refresh-token");

        // Database Verification 1: Verify user creation in DB
        var userInDb = await Context.Users
            .Include(u => u.RefreshTokens)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "newuser@example.com");

        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Ahmad Al-Mowafak");
        userInDb!.PasswordHash.Should().Be("HashedPassword123!");
        userInDb!.Role.Should().Be(UserRole.Customer);

        // Database Verification 2: Verify refresh token created & associated with user
        userInDb.RefreshTokens.Should().HaveCount(1);
        var createdRefreshToken = userInDb.RefreshTokens.First();
        createdRefreshToken!.Token.Should().Be("fake-refresh-token");
        createdRefreshToken!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}