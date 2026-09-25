using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Auth.Commands.Logout;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class LogoutEndpointTests : ApiTestBase
{
    public LogoutEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Logout_ShouldReturn401Unauthorized_WhenNoJwtTokenProvided()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new LogoutRequest("some-refresh-token");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn200OK_WhenUserIsAuthenticatedAndTokenIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Authenticated User", "authuser@example.com", "HashedPassword123", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        const string refreshTokenValue = "valid-refresh-token-to-revoke";
        var refreshToken = new RefreshToken(refreshTokenValue, DateTime.UtcNow.AddDays(7), user.Id);
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new LogoutRequest(refreshTokenValue);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_ShouldReturn400BadRequest_WhenRefreshTokenDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Authenticated User", "authuser@example.com", "HashedPassword123", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new LogoutRequest("non-existing-refresh-token");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}