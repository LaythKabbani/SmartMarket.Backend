using FluentAssertions;
using SmartMarket.Application.Features.Products.Commands.UpdateProduct;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandlerTests : TestBase
{
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new UpdateProductCommand(
            Guid.NewGuid(), "Updated Name", "Updated Desc", 150m, 20, Guid.NewGuid());

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

        var command = new UpdateProductCommand(
            Guid.NewGuid(), "Updated Name", "Updated Desc", 150m, 20, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorized()
    {
        // Arrange
        var owner = new User("Store Owner", "owner@example.com", "HashedPass123", UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var store = new Store("Smart Store", owner.Id, "Tech Store", "logo.png");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Old Name", "Old Desc", 100m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new UpdateProductCommand(
            product.Id, "Updated Name", "Updated Desc", 150m, 20, category.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to update this product.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNewCategoryDoesNotExist()
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

        var product = new Product("Old Name", "Old Desc", 100m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var nonExistingCategoryId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            product.Id, "Updated Name", "Updated Desc", 150m, 20, nonExistingCategoryId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("The specified category does not exist.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateProduct_WhenRequestIsValid()
    {
        var owner = new User("Store Owner", "owner@example.com", "HashedPass123", UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var store = new Store("Smart Store", owner.Id, "Tech Store", "logo.png");
        var oldCategory = new Category("Electronics", "Gadgets");
        var newCategory = new Category("Laptops", "Portable Computers");

        Context.Stores.Add(store);
        Context.Categories.AddRange(oldCategory, newCategory);
        await Context.SaveChangesAsync();

        var product = new Product("Old Name", "Old Desc", 100m, 10, store.Id, oldCategory.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new UpdateProductCommand(
            product.Id, "Updated Laptop", "New Specification", 2000m, 15, newCategory.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        Context.ChangeTracker.Clear();

        var updatedProduct = await Context.Products.FindAsync(product.Id);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("Updated Laptop");
        updatedProduct.Description.Should().Be("New Specification");
        updatedProduct.Price.Should().Be(2000m);
        updatedProduct.StockQuantity.Should().Be(15);
        updatedProduct.CategoryId.Should().Be(newCategory.Id);
    }
}