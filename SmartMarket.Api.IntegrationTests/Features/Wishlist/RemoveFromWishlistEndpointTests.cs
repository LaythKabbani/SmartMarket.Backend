using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Wishlist;

public class RemoveFromWishlistEndpointTests : ApiTestBase
{
    public RemoveFromWishlistEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RemoveFromWishlist_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.DeleteAsync($"/api/wishlist/items/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFromWishlist_ShouldReturn200OK_WhenItemExists()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        var storeOwner = new User("Store Owner", "owner@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.AddRange(customer, storeOwner);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", storeOwner.Id);
        var category = new Category("Electronics");
        DbContext.Stores.Add(store);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("Tablet", "Android Tablet", 400m, 20, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var wishlistItem = new WishlistItem(customer.Id, product.Id);
        DbContext.WishlistItems.Add(wishlistItem);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.DeleteAsync($"/api/wishlist/items/{product.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFromWishlist_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var factory = new SmartMarketWebApplicationFactory();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.DeleteAsync($"/api/wishlist/items/{Guid.NewGuid()}");
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}