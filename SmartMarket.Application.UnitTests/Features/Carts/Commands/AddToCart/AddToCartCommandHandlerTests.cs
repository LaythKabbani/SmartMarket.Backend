using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Carts.Commands.AddToCart;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Carts.Commands.AddToCart;

public class AddToCartCommandHandlerTests : TestBase
{
    private readonly AddToCartCommandHandler _handler;

    public AddToCartCommandHandlerTests()
    {
        _handler = new AddToCartCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        // 1. Arrange
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 2);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockIsInsufficient()
    {
        // 1. Arrange: Create dummy Store and Category IDs
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product("Coffee Beans", "Caffeine.", price: 15.0m, stockQuantity: 3, storeId, categoryId);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        // Try to request quantity = 5 (more than available)
        var command = new AddToCartCommand(Guid.NewGuid(), product.Id, 5);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Insufficient stock. Available: 3");
    }

    [Fact]
    public async Task Handle_ShouldCreateNewCartAndAddItem_WhenCartDoesNotExist()
    {
        // 1. Arrange
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product("Espresso Beans", "Rich taste", 20.0m, stockQuantity: 10, storeId, categoryId);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var command = new AddToCartCommand(userId, product.Id, 2);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        // Database Verification: Verify Cart and CartItem creation
        var cartInDb = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        cartInDb.Should().NotBeNull();
        cartInDb!.Id.Should().Be(result.Data);
        cartInDb!.Items.Should().HaveCount(1);

        var cartItem = cartInDb.Items.First();
        cartItem!.ProductId.Should().Be(product.Id);
        cartItem!.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingCart_WhenCartAlreadyExists()
    {
        // 1. Arrange
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product("Arabic Coffee", "Traditional cardamom blend", price: 12.5m, stockQuantity: 15, storeId, categoryId);
        Context.Products.Add(product);

        var existingCart = new Cart(userId);
        Context.Carts.Add(existingCart);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear(); // Clear the change tracker to simulate a fresh context

        var command = new AddToCartCommand(userId, product.Id, 3);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();

        Context.ChangeTracker.Clear();

        // Database Verification
        var updatedCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == existingCart.Id);

        updatedCart.Should().NotBeNull();
        updatedCart!.Items.Should().HaveCount(1);
        updatedCart!.Items.First().Quantity.Should().Be(3);
    }
}