using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Products.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Products;

public class GetProductByIdEndpointTests : ApiTestBase
{
    public GetProductByIdEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetProductById_ShouldReturn200OK_WhenProductExists()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Store 1", merchant.Id);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var category = new Category("Category 1");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("Sample Mouse", "Gaming Mouse", 25.0m, 50, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<ProductDto>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetProductById_ShouldReturn404NotFound_WhenProductDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<ProductDto>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}