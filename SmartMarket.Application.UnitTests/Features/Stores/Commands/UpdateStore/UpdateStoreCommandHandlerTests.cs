using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Stores.Commands.UpdateStore;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<UpdateStoreCommandHandler>> _loggerMock;
    private readonly UpdateStoreCommandHandler _handler;

    public UpdateStoreCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UpdateStoreCommandHandler>>();

        _handler = new UpdateStoreCommandHandler(
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

        var command = new UpdateStoreCommand(
            Guid.NewGuid(),
            "Updated Store Name",
            "Updated Description",
            "https://example.com/new-logo.png");

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

        var command = new UpdateStoreCommand(
            Guid.NewGuid(),
            "Updated Store Name",
            "Updated Description",
            "https://example.com/new-logo.png");

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
        var store = new Store("Old Store Name", ownerId, "Old Description", "old-logo.png");

        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new UpdateStoreCommand(
            store.Id,
            "Updated Store Name",
            "Updated Description",
            "https://example.com/new-logo.png");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to update this store.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateStoreDetailsAndReturnSuccess_WhenDataAndAuthorizationAreValid()
    {
        // 1. Arrange: User is authenticated AND authorized (isAuthorized: true)
        var userId = Guid.NewGuid().ToString();
        SetupUserContext(userId: userId, isAuthorized: true);

        var store = new Store("Old Store Name", Guid.Parse(userId), "Old Description", "old-logo.png");

        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new UpdateStoreCommand(
            store.Id,
            "Updated Store Name",
            "Updated Description",
            "https://example.com/new-logo.png");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Verify store details were updated in DB
        var updatedStoreInDb = await Context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == store.Id);

        updatedStoreInDb.Should().NotBeNull();
        updatedStoreInDb!.Name.Should().Be("Updated Store Name");
        updatedStoreInDb.Description.Should().Be("Updated Description");
        updatedStoreInDb.LogoUrl.Should().Be("https://example.com/new-logo.png");
    }
}