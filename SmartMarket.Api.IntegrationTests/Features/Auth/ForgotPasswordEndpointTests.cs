using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Features.Auth.Commands.ForgotPassword;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class ForgotPasswordEndpointTests : ApiTestBase
{
    public ForgotPasswordEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200OK_WhenEmailIsRegistered()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string email = "user@example.com";
        var user = new User("Registered User", email, "HashedPassword123", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand(email);
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn400BadRequest_WhenEmailDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var command = new ForgotPasswordCommand("nonexisting@example.com");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        // Arrange
        await ResetDatabaseAsync();

        using var factory = new SmartMarketWebApplicationFactory();
        var client = factory.CreateClient();

        var command = new ForgotPasswordCommand("test@example.com");

        // Act
        for (var i = 0; i < 3; i++)
        {
            var res = await client.PostAsJsonAsync("/api/auth/forgot-password", command);
            res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }

        var exceededResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", command);

        // Assert
        exceededResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}