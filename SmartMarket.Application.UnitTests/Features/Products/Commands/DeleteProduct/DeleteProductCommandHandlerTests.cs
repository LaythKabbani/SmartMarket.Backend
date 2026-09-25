using FluentAssertions;
using SmartMarket.Application.Features.Products.Commands.DeleteProduct;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandlerTests : TestBase
{
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _handler = new DeleteProductCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString());

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorized()
    {
        var owner = new User("Store Owner", "owner@example.com", "HashedPass123", UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(executingUserId, isAuthorized: false);

        var store = new Store("Smart Store", owner.Id, "Tech Store", "logo.png");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new DeleteProductCommand(product.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to delete this product.");
    }

    [Fact]
    public async Task Handle_ShouldMarkProductAsDeleted_WhenUserIsAuthorized()
    {
        var owner = new User("Store Owner", "owner@example.com", "HashedPass123", UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var store = new Store("Smart Store", owner.Id, "Tech Store", "logo.png");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new DeleteProductCommand(product.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var deletedProduct = await Context.Products.FindAsync(product.Id);
        deletedProduct.Should().NotBeNull();
        deletedProduct!.IsDeleted.Should().BeTrue();
    }
}