using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Carts.Commands.UpdateCartItemQuantity;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Carts.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandlerTests : TestBase
{
    private readonly UpdateCartItemQuantityCommandHandler _handler;

    public UpdateCartItemQuantityCommandHandlerTests()
    {
        _handler = new UpdateCartItemQuantityCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCartDoesNotExist()
    {
        // 1. Arrange
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 5);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cart not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotInCart()
    {
        // 1. Arrange: Create a cart for the user
        var userId = Guid.NewGuid();
        var cart = new Cart(userId);
        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new UpdateCartItemQuantityCommand(userId, Guid.NewGuid(), 3);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found in cart.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductDoesNotExistInDb()
    {
        // 1. Arrange: Create cart with item, but product record doesn't exist in DB
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(productId, 1);
        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new UpdateCartItemQuantityCommand(userId, productId, 2);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Requested quantity exceeds available stock.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRequestedQuantityExceedsStock()
    {
        // 1. Arrange: Product stock is 5, user requests 10
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var product = new Product("Turkish Coffee", "Dark roast", price: 10.0m, stockQuantity: 5, storeId, categoryId);
        Context.Products.Add(product);

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(product.Id, 2);
        Context.Carts.Add(cart);

        await Context.SaveChangesAsync();

        var command = new UpdateCartItemQuantityCommand(userId, product.Id, 10);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Requested quantity exceeds available stock.");
    }

    [Fact]
    public async Task Handle_ShouldUpdateQuantityAndReturnSuccess_WhenDataIsValidAndStockIsSufficient()
    {
        // 1. Arrange: Product stock is 10, user updates quantity from 2 to 6
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var product = new Product("Matcha Tea", "Organic green tea", price: 25.0m, stockQuantity: 10, storeId, categoryId);
        Context.Products.Add(product);

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(product.Id, 2);
        Context.Carts.Add(cart);

        await Context.SaveChangesAsync();

        var command = new UpdateCartItemQuantityCommand(userId, product.Id, 6);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Verify updated item quantity in DB
        var cartInDb = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        cartInDb.Should().NotBeNull();
        var updatedItem = cartInDb!.Items.FirstOrDefault(i => i.ProductId == product.Id);
        updatedItem.Should().NotBeNull();
        updatedItem!.Quantity.Should().Be(6);
    }
}