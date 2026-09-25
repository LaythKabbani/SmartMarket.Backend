using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Dtos;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Stores;

public class GetStoresEndpointTests : ApiTestBase
{
    public GetStoresEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetStores_ShouldReturn200OK_WithPaginatedList_WhenCalledByAnonymousUser()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Store Owner", "owner@example.com", "HashedPass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Sample Store", merchant.Id, "Description", "https://logo.com/logo.png");
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/stores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedListDto<StoreDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Sample Store");
    }
}