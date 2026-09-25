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

namespace SmartMarket.Api.IntegrationTests.Features.Categories;

public class UpdateCategoryEndpointTests : ApiTestBase
{
    public UpdateCategoryEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new UpdateCategoryRequest("Updated Name", "Updated Desc");
        var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn403Forbidden_WhenUserIsNotSuperAdmin()
    {
        // Arrange
        await ResetDatabaseAsync();

        var customerUser = new User("Normal Customer", "customer@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(customerUser);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customerUser);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateCategoryRequest("Updated Name", "Updated Desc");

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn200OK_WhenUserIsSuperAdminAndCategoryExists()
    {
        // Arrange
        await ResetDatabaseAsync();

        var category = new Category("Old Name", "Old Description");
        DbContext.Categories.Add(category);

        var adminUser = new User("Super Admin", "admin@example.com", "Password123!", UserRole.SuperAdmin);
        DbContext.Users.Add(adminUser);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(adminUser);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateCategoryRequest("New Name", "New Description");

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{category.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenCategoryDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var adminUser = new User("Super Admin", "admin@example.com", "Password123!", UserRole.SuperAdmin);
        DbContext.Users.Add(adminUser);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(adminUser);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateCategoryRequest("New Name", "New Description");

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}