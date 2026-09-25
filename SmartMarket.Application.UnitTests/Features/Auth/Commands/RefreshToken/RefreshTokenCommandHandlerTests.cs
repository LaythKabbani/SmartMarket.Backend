using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Auth.Commands.RefreshToken;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Settings;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests : TestBase
{
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock;
    private readonly JwtSettings _jwtSettings;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _jwtSettings = new JwtSettings
        {
            RefreshTokenExpiryDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);

        _handler = new RefreshTokenCommandHandler(
            Context,
            _jwtTokenGeneratorMock.Object,
            jwtOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenDoesNotBelongToAnyUser()
    {
        // 1. Arrange: Database has no matching token for any user
        var command = new RefreshTokenCommand("non-existent-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenIsInactiveOrRevoked()
    {
        // 1. Arrange: Create user with an inactive/revoked token
        var user = new User("Ahmad", "user@example.com", "HashedPassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var revokedToken = new Domain.Entities.RefreshToken("revoked-token", DateTime.UtcNow.AddDays(7), user.Id);
        revokedToken.Revoke(); // Token becomes inactive

        Context.RefreshTokens.Add(revokedToken);
        await Context.SaveChangesAsync();

        var command = new RefreshTokenCommand("revoked-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Refresh token has expired or been revoked.");
    }

    [Fact]
    public async Task Handle_ShouldRefreshTokensAndRevokeOldToken_WhenTokenIsValid()
    {
        // 1. Arrange: Create user with a valid active refresh token
        var user = new User("Ahmad", "valid@example.com", "HashedPassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var activeToken = new Domain.Entities.RefreshToken("valid-old-token", DateTime.UtcNow.AddDays(7), user.Id);
        Context.RefreshTokens.Add(activeToken);
        await Context.SaveChangesAsync();

        // Setup token generator mock outputs
        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns("new-access-token");

        _jwtTokenGeneratorMock
            .Setup(g => g.GenerateRefreshToken())
            .Returns("new-refresh-token");

        var command = new RefreshTokenCommand("valid-old-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");
        result.Data!.Email.Should().Be("valid@example.com");

        // Database Verification 1: Verify old token is now revoked
        var oldTokenInDb = await Context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == "valid-old-token");

        oldTokenInDb.Should().NotBeNull();
        oldTokenInDb!.IsRevoked.Should().BeTrue();

        // Database Verification 2: Verify new refresh token was created & saved
        var newTokenInDb = await Context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == "new-refresh-token");

        newTokenInDb.Should().NotBeNull();
        newTokenInDb!.UserId.Should().Be(user.Id);
        newTokenInDb!.IsRevoked.Should().BeFalse();
        newTokenInDb!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}