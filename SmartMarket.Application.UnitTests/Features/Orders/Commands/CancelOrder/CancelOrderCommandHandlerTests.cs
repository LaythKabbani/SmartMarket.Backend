using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Orders.Commands.CancelOrder;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<CancelOrderCommandHandler>> _loggerMock;
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CancelOrderCommandHandler>>();

        _handler = new CancelOrderCommandHandler(
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

        var command = new CancelOrderCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderDoesNotExist()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString());

        var command = new CancelOrderCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorizedToCancelOrder()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var order = new Order(Guid.NewGuid(), 150m, "Damascus, Syria");
        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to cancel this order.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderStatusIsNotPending()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var order = new Order(Guid.NewGuid(), 150m, "Damascus, Syria");
        order.UpdateStatus(OrderStatus.Delivered);

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only pending orders can be cancelled.");
    }

    [Fact]
    public async Task Handle_ShouldCancelOrderAndRestoreProductStock_WhenCommandIsValid()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var store = new Store("Smart Store", ownerId, "Tech Store", "https://logo.url");
        var category = new Category("Electronics", "Gadgets");

        var product1 = new Product("Laptop", "Tech", 1000m, stockQuantity: 5, store.Id, category.Id);
        var product2 = new Product("Mouse", "Tech", 20m, stockQuantity: 10, store.Id, category.Id);

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        Context.Products.AddRange(product1, product2);

        var order = new Order(userId, 2060m, "Damascus, Syria");

        var item1 = new OrderItem(order.Id, product1.Id, quantity: 2, unitPrice: 1000m);
        var item2 = new OrderItem(order.Id, product2.Id, quantity: 3, unitPrice: 20m);

        order.Items.Add(item1);
        order.Items.Add(item2);

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        Context.ChangeTracker.Clear();

        var updatedOrder = await Context.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);

        var updatedProduct1 = await Context.Products.FindAsync(product1.Id);
        var updatedProduct2 = await Context.Products.FindAsync(product2.Id);

        updatedProduct1!.StockQuantity.Should().Be(7);  // 5 + 2
        updatedProduct2!.StockQuantity.Should().Be(13); // 10 + 3
    }
}