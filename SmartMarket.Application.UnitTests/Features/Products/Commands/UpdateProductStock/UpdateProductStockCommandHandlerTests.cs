using FluentAssertions;
using SmartMarket.Application.Features.Products.Commands.UpdateProductStock;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Commands.UpdateProductStock;

public class UpdateProductStockCommandHandlerTests : TestBase
{
    private readonly UpdateProductStockCommandHandler _handler;

    public UpdateProductStockCommandHandlerTests()
    {
        _handler = new UpdateProductStockCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new UpdateProductStockCommand(Guid.NewGuid(), 50);

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

        var command = new UpdateProductStockCommand(Guid.NewGuid(), 50);

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
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var owner = new User("Fullname", "example@gmail.com", "PasswordHash123!", Domain.Enums.UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var store = new Store("Smart Store", owner.Id, "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new UpdateProductStockCommand(product.Id, 50);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to update this product's stock.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockQuantityIsNegative()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var owner = new User("Fullname", "example@gmail.com", "PasswordHash123!", Domain.Enums.UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var store = new Store("Smart Store", owner.Id, "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new UpdateProductStockCommand(product.Id, -5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Stock quantity cannot be negative.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductStock_WhenRequestIsValid()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var owner = new User("Fullname", "example@gmail.com", "PasswordHash123!", Domain.Enums.UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var store = new Store("Smart Store", owner.Id, "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var newStockQuantity = 25;
        var command = new UpdateProductStockCommand(product.Id, newStockQuantity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        Context.ChangeTracker.Clear();

        var updatedProduct = await Context.Products.FindAsync(product.Id);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.StockQuantity.Should().Be(newStockQuantity);
    }
}