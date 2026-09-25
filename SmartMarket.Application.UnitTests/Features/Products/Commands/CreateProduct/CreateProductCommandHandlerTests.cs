using FluentAssertions;
using SmartMarket.Application.Features.Products.Commands.CreateProduct;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandlerTests : TestBase
{
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(
            Context,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new CreateProductCommand(
            "Laptop", "Gaming Laptop", 1200m, 10, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStoreDoesNotExist()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString());

        var command = new CreateProductCommand(
            "Laptop", "Gaming Laptop", 1200m, 10, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("The specified store does not exist.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorizedToAddProductToStore()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var store = new Store("Smart Store", Guid.NewGuid(), "Tech Store");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new CreateProductCommand(
            "Laptop", "Gaming Laptop", 1200m, 10, store.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to add products to this store.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var store = new Store("Smart Store", Guid.NewGuid(), "Tech Store");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new CreateProductCommand(
            "Laptop", "Gaming Laptop", 1200m, 10, store.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("The specified category does not exist.");
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var store = new Store("Smart Store", Guid.NewGuid(), "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var command = new CreateProductCommand(
            "Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        Context.ChangeTracker.Clear();

        var createdProduct = await Context.Products.FindAsync(result.Data);
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be("Laptop");
        createdProduct.Price.Should().Be(1200m);
        createdProduct.StockQuantity.Should().Be(10);
        createdProduct.StoreId.Should().Be(store.Id);
        createdProduct.CategoryId.Should().Be(category.Id);
    }
}