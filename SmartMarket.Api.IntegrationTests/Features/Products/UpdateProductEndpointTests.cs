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

namespace SmartMarket.Api.IntegrationTests.Features.Products;

public class UpdateProductEndpointTests : ApiTestBase
{
    public UpdateProductEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var request = new UpdateProductRequest("Name", "Desc", 100m, 10, Guid.NewGuid());

        // Act
        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn403Forbidden_WhenUserIsNotMerchant()
    {
        // Arrange
        await ResetDatabaseAsync();

        var customer = new User("Customer", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateProductRequest("Name", "Desc", 100m, 10, Guid.NewGuid());

        // Act
        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn200OK_WhenMerchantRequestIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Merchant Store", merchant.Id);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var category = new Category("Category");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("Old Name", "Old Desc", 10m, 5, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateProductRequest("New Name", "New Desc", 20m, 10, category.Id);

        // Act
        var response = await client.PutAsJsonAsync($"/api/products/{product.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn400BadRequest_WhenProductDoesNotExistOrValidationFails()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateProductRequest("New Name", "New Desc", 20m, 10, Guid.NewGuid());

        // Act
        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}