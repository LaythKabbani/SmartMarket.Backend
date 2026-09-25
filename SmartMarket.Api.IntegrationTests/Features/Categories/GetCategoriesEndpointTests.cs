using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Categories;

public class GetCategoriesEndpointTests : ApiTestBase
{
    public GetCategoriesEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetCategories_ShouldReturn200OK_WithoutAuthentication()
    {
        // Arrange
        await ResetDatabaseAsync();

        DbContext.Categories.Add(new Category("Electronics", "Electronic Devices"));
        DbContext.Categories.Add(new Category("Books", "Printed and Electronic Books"));
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
    }
}