using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Wishlist.Commands.RemoveFromWishlist;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Wishlist.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandlerTests : TestBase
{
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly RemoveFromWishlistCommandHandler _handler;

    public RemoveFromWishlistCommandHandlerTests()
    {
        _authorizationServiceMock = new Mock<IAuthorizationService>();

        _handler = new RemoveFromWishlistCommandHandler(
            Context,
            _authorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new RemoveFromWishlistCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenWishlistItemNotFound()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Customer);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var command = new RemoveFromWishlistCommand(user.Id, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAuthorizationFails()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Customer);
        var user2 = new User("Test User 2", "test2user@market.com", "hashedpassword2", UserRole.Customer);
        var storeOwner = new User("Store Owner", "owner@gmail.com", "apsafdaa", UserRole.Merchant);

        Context.Users.AddRange(user, user2, storeOwner);
        await Context.SaveChangesAsync();

        var store = new Store("Test Store", storeOwner.Id, "description", "logo.png");
        var category = new Category("Electronics", "electronics.png");
        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("product test", "description", 150.00m, 20, store.Id, category.Id, "product.img");
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var wishlistItem = new WishlistItem(user2.Id, product.Id);
        Context.WishlistItems.Add(wishlistItem);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var command = new RemoveFromWishlistCommand(user2.Id, product.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Not authorized.");
    }

    [Fact]
    public async Task Handle_ShouldRemoveFromWishlist_WhenRequestIsValid()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Customer);
        var storeOwner = new User("Store Owner", "owner@gmail.com", "apsafdaa", UserRole.Merchant);

        Context.Users.AddRange(user, storeOwner);
        await Context.SaveChangesAsync();

        var store = new Store("Test Store", storeOwner.Id, "description", "logo.png");
        var category = new Category("Electronics", "electronics.png");
        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("product test", "description", 150.00m, 20, store.Id, category.Id, "product.img");
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var wishlistItem = new WishlistItem(user.Id, product.Id);
        Context.WishlistItems.Add(wishlistItem);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new RemoveFromWishlistCommand(user.Id, product.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var removedItem = await Context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == product.Id);
        removedItem.Should().BeNull();
    }
}