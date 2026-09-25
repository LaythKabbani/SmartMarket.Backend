using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Auth.Commands.Register;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class RegisterEndpointTests : ApiTestBase
{
    public RegisterEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Register_ShouldReturn200OK_WhenDataIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new RegisterRequest(
            "New User",
            "newuser@example.com",
            "Password123!");

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be("newuser@example.com");
        result.Data.FullName.Should().Be("New User");
        result.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ShouldReturn400BadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string existingEmail = "existing@example.com";
        var existingUser = new User("Existing User", existingEmail, "HashedPassword123", Domain.Enums.UserRole.Customer);
        DbContext.Users.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var request = new RegisterRequest(
            "Another User",
            existingEmail,
            "Password123!");

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Register_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        // Arrange
        await ResetDatabaseAsync();

        /*
         * AuthRateLimit:
         * PermitLimit = 3
         * Window = 5 minutes
         */
        using var factory = new SmartMarketWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new RegisterRequest("Test User", "test@example.com", "Password123!");

        // Act
        for (var i = 0; i < 3; i++)
        {
            var res = await client.PostAsJsonAsync("/api/auth/register", request);
            res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }

        var exceededResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        exceededResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}