using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.WebApi.Controllers;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Users;

public class UpdateUserEndpointTests : ApiTestBase
{
    public UpdateUserEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var request = new UpdateUserRequest("Updated Name", UserRole.Merchant);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn200OK_WhenUserIsAuthenticatedAndValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Original User", "user@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new UpdateUserRequest("Updated User Name", UserRole.Merchant);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{user.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }
}