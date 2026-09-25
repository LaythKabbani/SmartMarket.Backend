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
using SmartMarket.Application.Features.Orders.Commands.CreateOrder;

namespace SmartMarket.Api.IntegrationTests.Features.Orders;

public class UpdateOrderStatusEndpointTests : ApiTestBase
{
    public UpdateOrderStatusEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new UpdateOrderStatusRequest(OrderStatus.Processing);
        var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync($"/api/orders/{Guid.NewGuid()}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn403Forbidden_WhenUserIsCustomer()
    {
        // Arrange
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateOrderStatusRequest(OrderStatus.Processing);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/orders/{Guid.NewGuid()}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRole.Merchant)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task UpdateStatus_ShouldReturn200OK_WhenUserHasPermissionAndOrderExists(UserRole role)
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Authorized User", $"authorized_{role}@example.com", "Password123!", role);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", user.Id);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var category = new Category("Test Category");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("Test Product", "Description", 50.0m, 10, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var order = new Order(user.Id, 100.0m, "Address");

        order.Items.Add(new OrderItem(order.Id, product.Id, 2, product.Price));

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateOrderStatusRequest(OrderStatus.Shipped);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn400BadRequest_WhenOrderDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@example.com", "Password123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateOrderStatusRequest(OrderStatus.Delivered);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/orders/{Guid.NewGuid()}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}