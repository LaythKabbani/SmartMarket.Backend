using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Orders.Commands.CreateOrder;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();

        _handler = new CreateOrderCommandHandler(
            Context,
            CurrentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIdIsNotValidGuid()
    {
        // Arrange
        SetupUserContext(userId: "invalid-guid");

        var command = new CreateOrderCommand(Guid.NewGuid(), "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserTriesToCreateOrderForAnotherUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        SetupUserContext(currentUserId.ToString());

        var command = new CreateOrderCommand(anotherUserId, "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You can only create orders for your own account.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCartIsEmptyOrNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString());

        var command = new CreateOrderCommand(userId, "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cart is empty. Add products to cart before checkout.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductInCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString());

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(Guid.NewGuid(), 2);

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new CreateOrderCommand(userId, "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("One or more products in the cart are no longer available.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString());

        var store = new Store("Smart Store", Guid.NewGuid(), "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        var product = new Product("Laptop", "Tech", 1000m, stockQuantity: 2, store.Id, category.Id);

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        Context.Products.Add(product);

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(product.Id, 5);

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new CreateOrderCommand(userId, "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_DeductStock_AndClearCart_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString());

        var store = new Store("Smart Store", Guid.NewGuid(), "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        var product1 = new Product("Laptop", "Tech", 1000m, stockQuantity: 10, store.Id, category.Id);
        var product2 = new Product("Mouse", "Tech", 20m, stockQuantity: 15, store.Id, category.Id);

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        Context.Products.AddRange(product1, product2);

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(product1.Id, 2); // 2 * 1000 = 2000
        cart.AddOrUpdateItem(product2.Id, 3); // 3 * 20 = 60

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new CreateOrderCommand(userId, "Damascus, Syria");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        Context.ChangeTracker.Clear();

        var createdOrder = await Context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Data);

        createdOrder.Should().NotBeNull();
        createdOrder!.UserId.Should().Be(userId);
        createdOrder.TotalAmount.Should().Be(2060m);
        createdOrder.Items.Should().HaveCount(2);

        var updatedProduct1 = await Context.Products.FindAsync(product1.Id);
        var updatedProduct2 = await Context.Products.FindAsync(product2.Id);

        updatedProduct1!.StockQuantity.Should().Be(8);  // 10 - 2
        updatedProduct2!.StockQuantity.Should().Be(12); // 15 - 3

        var updatedCart = await Context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        updatedCart!.Items.Should().BeEmpty();
    }
}