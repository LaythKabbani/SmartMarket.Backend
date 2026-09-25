using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Stores.Commands.CreateStore;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<CreateStoreCommandHandler>> _loggerMock;
    private readonly CreateStoreCommandHandler _handler;

    public CreateStoreCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CreateStoreCommandHandler>>();

        _handler = new CreateStoreCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new CreateStoreCommand("Tech Store", Guid.NewGuid(), "Description", "http://logo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIdIsNotValidGuid()
    {
        // Arrange
        SetupUserContext(userId: "invalid-guid", isAuthenticated: true);

        var command = new CreateStoreCommand("Tech Store", Guid.NewGuid(), "Description", "http://logo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserFailsAuthorizationRequirement()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        SetupUserContext(currentUserId.ToString(), isAuthorized: false);

        var command = new CreateStoreCommand("Tech Store", currentUserId, "Description", "http://logo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only merchants and admins are authorized to create stores.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOwnerAlreadyHasAStore()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        SetupUserContext(currentUserId.ToString(), isAuthorized: true);

        var existingStore = new Store("Existing Store", currentUserId, "Existing Desc", "http://logo.url");
        Context.Stores.Add(existingStore);
        await Context.SaveChangesAsync();

        var command = new CreateStoreCommand("New Store", currentUserId, "New Desc", "http://newlogo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You already own a registered store. Each merchant is limited to one store only.");
    }

    [Fact]
    public async Task Handle_ShouldCreateStore_WhenMerchantCreatesOwnStore()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        SetupUserContext(currentUserId.ToString(), role: UserRole.Merchant.ToString(), isAuthorized: true);

        var requestedOwnerId = Guid.NewGuid();
        var command = new CreateStoreCommand("Merchant Store", requestedOwnerId, "Store Description", "http://logo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var createdStore = await Context.Stores.FindAsync(result.Data);
        createdStore.Should().NotBeNull();
        createdStore!.Name.Should().Be("Merchant Store");
        createdStore.OwnerId.Should().Be(currentUserId);
    }

    [Fact]
    public async Task Handle_ShouldCreateStore_WhenSuperAdminCreatesStoreForAnotherOwner()
    {
        // Arrange
        var superAdminUserId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        SetupUserContext(superAdminUserId.ToString(), role: UserRole.SuperAdmin.ToString(), isAuthorized: true);

        var command = new CreateStoreCommand("SuperAdmin Store", targetOwnerId, "Admin Desc", "http://logo.url");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var createdStore = await Context.Stores.FindAsync(result.Data);
        createdStore.Should().NotBeNull();
        createdStore!.Name.Should().Be("SuperAdmin Store");
        createdStore.OwnerId.Should().Be(targetOwnerId);
    }
}