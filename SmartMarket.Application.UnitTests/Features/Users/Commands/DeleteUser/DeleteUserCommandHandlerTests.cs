using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Users.Commands.DeleteUser;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<DeleteUserCommandHandler>> _loggerMock;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DeleteUserCommandHandler>>();

        _handler = new DeleteUserCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // 1. Arrange: User is not authenticated
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new DeleteUserCommand(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTargetUserDoesNotExist()
    {
        // 1. Arrange: Authenticated user, but target user is not in DB
        SetupUserContext(userId: Guid.NewGuid().ToString());

        var command = new DeleteUserCommand(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorizedByPolicy()
    {
        // 1. Arrange: User is authenticated but NOT authorized (isAuthorized: false)
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, isAuthorized: false);

        var targetUser = new User("Target User", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        var command = new DeleteUserCommand(targetUser.Id);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to delete this user account.");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteUserAndReturnSuccess_WhenAuthorizationSucceeds()
    {
        // 1. Arrange: User is authenticated AND authorized (isAuthorized: true)
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, isAuthorized: true);

        var targetUser = new User("Target User", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        var command = new DeleteUserCommand(targetUser.Id);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Ensure soft-delete state (IsDeleted flag)
        var userInDb = await Context.Users.FindAsync(targetUser.Id);
        userInDb.Should().NotBeNull();
        userInDb!.IsDeleted.Should().BeTrue();
    }
}