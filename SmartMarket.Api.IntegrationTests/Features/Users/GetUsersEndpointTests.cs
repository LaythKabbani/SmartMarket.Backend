using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Dtos;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Users;

public class GetUsersEndpointTests : ApiTestBase
{
    public GetUsersEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetUsers_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_ShouldReturn403Forbidden_WhenUserIsNotSuperAdmin()
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

        // Act
        var response = await client.GetAsync("/api/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ShouldReturn200OK_WhenUserIsSuperAdmin()
    {
        // Arrange
        await ResetDatabaseAsync();

        var admin = new User("Admin User", "admin@example.com", "Password123!", UserRole.SuperAdmin);
        var user1 = new User("User One", "user1@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.AddRange(admin, user1);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(admin);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.GetAsync("/api/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedListDto<UserDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }
}