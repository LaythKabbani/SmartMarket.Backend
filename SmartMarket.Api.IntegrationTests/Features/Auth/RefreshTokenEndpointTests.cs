using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Auth.Commands.RefreshToken;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class RefreshTokenEndpointTests : ApiTestBase
{
    public RefreshTokenEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RefreshToken_ShouldReturn200OK_WhenRefreshTokenIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Token User", "tokenuser@example.com", "HashedPassword123", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        const string validToken = "valid-refresh-token-123";
        var refreshToken = new RefreshToken(validToken, DateTime.UtcNow.AddDays(7), user.Id);
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest(validToken);
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturn400BadRequest_WhenRefreshTokenIsInvalidOrExpired()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new RefreshTokenRequest("invalid-or-expired-token");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}