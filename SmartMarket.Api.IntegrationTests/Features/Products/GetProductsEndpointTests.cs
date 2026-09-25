using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Dtos;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Products.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Products;

public class GetProductsEndpointTests : ApiTestBase
{
    public GetProductsEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_ShouldReturn200OK_WithPaginatedList_WhenCalledByAnonymousUser()
    {
        // Arrange
        await ResetDatabaseAsync();

        var category = new Category("Category 1");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var merchant = new User("Merchant", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Store 1", merchant.Id);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var product = new Product("Test Product", "Desc", 10.0m, 10, store.Id, category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedListDto<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }
}