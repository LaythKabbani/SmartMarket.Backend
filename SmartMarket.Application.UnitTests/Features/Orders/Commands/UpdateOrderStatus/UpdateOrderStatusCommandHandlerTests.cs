using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Features.Orders.Commands.UpdateOrderStatus;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Orders.Commands.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandlerTests : TestBase
{
    private readonly Mock<ILogger<UpdateOrderStatusCommandHandler>> _loggerMock;
    private readonly UpdateOrderStatusCommandHandler _handler;

    public UpdateOrderStatusCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UpdateOrderStatusCommandHandler>>();

        _handler = new UpdateOrderStatusCommandHandler(
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

        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Shipped);

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

        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorized()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var order = new Order(Guid.NewGuid(), 100m, "Damascus, Syria");

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to update this order's status.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderIsAlreadyCancelled()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var order = new Order(Guid.NewGuid(), 100m, "Damascus, Syria");
        order.UpdateStatus(OrderStatus.Cancelled);

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot change status of a cancelled order.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatus_WhenStatusIsNormalChange()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var order = new Order(Guid.NewGuid(), 100m, "Damascus, Syria");

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        Context.ChangeTracker.Clear();

        var updatedOrder = await Context.Orders.FindAsync(order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Handle_ShouldRestoreProductStock_WhenNewStatusIsCancelled()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var ownerId = Guid.NewGuid();

        var store = new Store("Smart Store", ownerId, "Tech Store", "https://logo.url");
        var category = new Category("Electronics", "Gadgets");

        var product1 = new Product("Laptop", "Tech", 1000m, stockQuantity: 5, store.Id, category.Id);
        var product2 = new Product("Mouse", "Tech", 20m, stockQuantity: 10, store.Id, category.Id);

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        Context.Products.AddRange(product1, product2);

        var order = new Order(Guid.NewGuid(), 2060m, "Damascus, Syria");

        var item1 = new OrderItem(order.Id, product1.Id, quantity: 2, unitPrice: 1000m);
        var item2 = new OrderItem(order.Id, product2.Id, quantity: 3, unitPrice: 20m);

        order.Items.Add(item1);
        order.Items.Add(item2);

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Cancelled);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        Context.ChangeTracker.Clear();

        var updatedOrder = await Context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);

        var updatedProduct1 = await Context.Products.FindAsync(product1.Id);
        var updatedProduct2 = await Context.Products.FindAsync(product2.Id);

        updatedProduct1!.StockQuantity.Should().Be(7);  // 5 + 2
        updatedProduct2!.StockQuantity.Should().Be(13); // 10 + 3
    }
}