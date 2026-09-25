using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Users;

public class GetUserByIdEndpointTests : ApiTestBase
{
    public GetUserByIdEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetUserById_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ShouldReturn200OK_WhenUserExistsAndAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();

        var targetUser = new User("Target User", "target@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(targetUser);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(targetUser);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.GetAsync($"/api/users/{targetUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<UserDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Id.Should().Be(targetUser.Id);
    }

    [Fact]
    public async Task GetUserById_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Auth User", "auth@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}