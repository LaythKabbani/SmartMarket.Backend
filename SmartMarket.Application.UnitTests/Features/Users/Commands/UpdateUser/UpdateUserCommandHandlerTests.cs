using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Users.Commands.UpdateUser;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<UpdateUserCommandHandler>> _loggerMock;
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UpdateUserCommandHandler>>();

        _handler = new UpdateUserCommandHandler(
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

        var command = new UpdateUserCommand(Guid.NewGuid(), "New Name", UserRole.Customer);

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

        var command = new UpdateUserCommand(Guid.NewGuid(), "New Name", UserRole.Customer);

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

        var targetUser = new User("Old Name", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        var command = new UpdateUserCommand(targetUser.Id, "New Name", UserRole.Customer);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to update this user profile.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateProfileAndKeepOldRole_WhenUserIsNotSuperAdmin()
    {
        // 1. Arrange: Regular user (e.g. Customer) updating their own profile
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, role: UserRole.Customer.ToString(), isAuthorized: true);

        var targetUser = new User("Old Name", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        // Attempting to request a role upgrade to Admin
        var command = new UpdateUserCommand(targetUser.Id, "Updated Name", UserRole.SuperAdmin);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // DB Verification: Name updated, but Role remains unchanged (Customer)
        var userInDb = await Context.Users.FindAsync(targetUser.Id);
        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Updated Name");
        userInDb.Role.Should().Be(UserRole.Customer);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProfileAndRole_WhenUserIsSuperAdmin()
    {
        // 1. Arrange: Executing user is a SuperAdmin
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, role: UserRole.SuperAdmin.ToString(), isAuthorized: true);

        var targetUser = new User("Old Name", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        // SuperAdmin upgrading user role to Admin
        var command = new UpdateUserCommand(targetUser.Id, "Updated Name", UserRole.SuperAdmin);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // DB Verification: Name and Role updated
        var userInDb = await Context.Users.FindAsync(targetUser.Id);
        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Updated Name");
        userInDb.Role.Should().Be(UserRole.SuperAdmin);
    }
}