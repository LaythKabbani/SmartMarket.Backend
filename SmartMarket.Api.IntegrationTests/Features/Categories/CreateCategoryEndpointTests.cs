using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Categories.Commands.CreateCategory;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Categories;

public class CreateCategoryEndpointTests : ApiTestBase
{
    public CreateCategoryEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        await ResetDatabaseAsync();

        var command = new CreateCategoryCommand("Hardware", "PC Parts");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn403Forbidden_WhenUserIsNotSuperAdmin()
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

        var command = new CreateCategoryCommand("Hardware", "PC Parts");

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn200OK_WhenUserIsSuperAdminAndDataIsValid()
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

        var command = new CreateCategoryCommand("Hardware", "PC Parts");

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }
}