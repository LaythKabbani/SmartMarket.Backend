using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Carts.Commands.RemoveFromCart;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Carts.Commands.RemoveFromCart;

public class RemoveFromCartCommandHandlerTests : TestBase
{
    private readonly RemoveFromCartCommandHandler _handler;

    public RemoveFromCartCommandHandlerTests()
    {
        _handler = new RemoveFromCartCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCartDoesNotExist()
    {
        // 1. Arrange
        var command = new RemoveFromCartCommand(Guid.NewGuid(), Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cart not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotInCart()
    {
        // 1. Arrange: Create a cart for user without adding the requested product
        var userId = Guid.NewGuid();
        var cart = new Cart(userId);
        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new RemoveFromCartCommand(userId, Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found in cart.");
    }

    [Fact]
    public async Task Handle_ShouldRemoveItemAndReturnSuccess_WhenItemExistsInCart()
    {
        // 1. Arrange: Create cart and add an item to it
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(productId, 3);

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var command = new RemoveFromCartCommand(userId, productId);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Database Verification: Verify item was removed from cart
        var cartInDb = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        cartInDb.Should().NotBeNull();
        cartInDb!.Items.Should().BeEmpty();
    }
}