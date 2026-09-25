using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Categories;

public class GetCategoryByIdEndpointTests : ApiTestBase
{
    public GetCategoryByIdEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturn200OK_WhenCategoryExists()
    {
        // Arrange
        await ResetDatabaseAsync();

        var category = new Category("Groceries", "Food and beverages");
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<CategoryDto>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(category.Id);
        result.Data.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<CategoryDto>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}