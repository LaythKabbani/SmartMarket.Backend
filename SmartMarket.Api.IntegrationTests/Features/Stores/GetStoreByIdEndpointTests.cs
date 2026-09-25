using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Stores;

public class GetStoreByIdEndpointTests : ApiTestBase
{
    public GetStoreByIdEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetStoreById_ShouldReturn200OK_WhenStoreExists()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@example.com", "Password123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Tech Zone", merchant.Id, "Tech Products", "https://logo.com/tech.png");
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/stores/{store.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<StoreDto>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(store.Id);
        result.Data.Name.Should().Be("Tech Zone");
    }

    [Fact]
    public async Task GetStoreById_ShouldReturn404NotFound_WhenStoreDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/stores/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<StoreDto>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}