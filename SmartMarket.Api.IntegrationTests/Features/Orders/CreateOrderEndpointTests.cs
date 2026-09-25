using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.WebApi.Controllers;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Orders;

public class CreateOrderEndpointTests : ApiTestBase
{
    public CreateOrderEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new CreateOrderRequest("123 Main St, City");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn200OK_WhenCartHasItemsAndRequestIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var category = new Category("Sample Category");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var user = new User("Customer User", "customer@example.com", "HashedPassword123", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var store = new Store("Sample Store", user.Id, "Store Description");
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var product = new Product("Sample Product", "Description", 50.0m, 100, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var cart = new Cart(user.Id);
        cart.AddOrUpdateItem(product.Id, 2);
        DbContext.Carts.Add(cart);

        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateOrderRequest("123 Street, City");

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn400BadRequest_WhenCartIsEmpty()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Empty Cart User", "emptycart@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateOrderRequest("123 Street, City");

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}