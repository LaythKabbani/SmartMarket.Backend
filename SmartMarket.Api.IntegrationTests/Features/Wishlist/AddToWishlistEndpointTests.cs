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
using SmartMarket.WebApi.Controllers;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Wishlist;

public class AddToWishlistEndpointTests : ApiTestBase
{
    public AddToWishlistEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AddToWishlist_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();
        var request = new AddToWishlistRequest(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/wishlist/add", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToWishlist_ShouldReturn200OK_WhenRequestIsValid()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        var storeOwner = new User("Store Owner", "owner@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.AddRange(customer, storeOwner);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", storeOwner.Id, "description", "logo.png");
        var category = new Category("Electronics", "description");
        DbContext.Stores.Add(store);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("Phone", "Smartphone", 800m, 15, store.Id, category.Id, "image.png");
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new AddToWishlistRequest(product.Id);

        var response = await client.PostAsJsonAsync("/api/wishlist/add", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddToWishlist_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
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

        var request = new AddToWishlistRequest(Guid.NewGuid());
        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/wishlist/add", request);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}