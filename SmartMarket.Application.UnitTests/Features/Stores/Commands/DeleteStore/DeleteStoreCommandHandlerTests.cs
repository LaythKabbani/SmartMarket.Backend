using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Stores.Commands.DeleteStore;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Stores.Commands.DeleteStore;

public class DeleteStoreCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<DeleteStoreCommandHandler>> _loggerMock;
    private readonly DeleteStoreCommandHandler _handler;

    public DeleteStoreCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DeleteStoreCommandHandler>>();

        _handler = new DeleteStoreCommandHandler(
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

        var command = new DeleteStoreCommand(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStoreDoesNotExist()
    {
        // 1. Arrange: Authenticated user, but store does not exist in DB
        SetupUserContext(userId: Guid.NewGuid().ToString());

        var command = new DeleteStoreCommand(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Store not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorizedByPolicy()
    {
        // 1. Arrange: User is authenticated but NOT authorized (isAuthorized: false)
        var userId = Guid.NewGuid().ToString();
        SetupUserContext(userId: userId, isAuthorized: false);

        var ownerId = Guid.NewGuid();
        var store = new Store("Test Store", ownerId, "Description", "logo.jpg");

        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new DeleteStoreCommand(store.Id);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to delete this store.");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteStoreAndReturnSuccess_WhenAuthorizationSucceeds()
    {
        // 1. Arrange: User is authenticated AND authorized (isAuthorized: true)
        var userId = Guid.NewGuid().ToString();
        SetupUserContext(userId: userId, isAuthorized: true);

        var store = new Store("Test Store", Guid.Parse(userId), "Description", "logo.jpg");

        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new DeleteStoreCommand(store.Id);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Verify soft-delete state
        var storeInDb = await Context.Stores.FindAsync(store.Id);
        storeInDb.Should().NotBeNull();
        storeInDb!.IsDeleted.Should().BeTrue();
    }
}