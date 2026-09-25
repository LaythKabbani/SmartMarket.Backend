using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Auth.Commands.Logout;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Auth.Commands.Logout;

public class LogoutCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<LogoutCommandHandler>> _loggerMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<LogoutCommandHandler>>();

        _handler = new LogoutCommandHandler(Context, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefreshTokenNotFoundOrUserMismatch()
    {
        // 1. Arrange: Database is empty (token does not exist or user mismatch)
        var command = new LogoutCommand(Guid.NewGuid(), "non-existent-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid refresh token or token does not belong to the user.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenIsAlreadyRevoked()
    {
        // 1. Arrange: Create a user and a token that is already revoked
        var userId = Guid.NewGuid();
        var refreshToken = new Domain.Entities.RefreshToken("already-revoked-token", DateTime.UtcNow.AddDays(7), userId);

        // Revoke the token using domain method before saving
        refreshToken.Revoke();

        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        var command = new LogoutCommand(userId, "already-revoked-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Token has already been revoked.");
    }

    [Fact]
    public async Task Handle_ShouldRevokeTokenAndReturnSuccess_WhenTokenIsValid()
    {
        // 1. Arrange: Create a valid active refresh token
        var userId = Guid.NewGuid();
        var refreshToken = new Domain.Entities.RefreshToken("valid-token", DateTime.UtcNow.AddDays(7), userId);

        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        var command = new LogoutCommand(userId, "valid-token");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Verify token status updated to revoked in DB
        var updatedToken = await Context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == "valid-token" && rt.UserId == userId);

        updatedToken.Should().NotBeNull();
        updatedToken!.IsRevoked.Should().BeTrue();
    }
}