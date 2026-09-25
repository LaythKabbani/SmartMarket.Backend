using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Auth.Commands.Login;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class LoginEndpointTests : ApiTestBase
{
    public LoginEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_ShouldReturn200OK_WhenCredentialsAreValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string email = "valid@example.com";
        const string password = "Password123!";

        var passwordHasher = Factory.Services.GetRequiredService<IPasswordHasher>();

        var passwordHash = passwordHasher.HashPassword(password);

        var user = new User(
            "Valid User",
            email,
            passwordHash,
            UserRole.Customer);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest(
            email,
            password);

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        result.Data!.Email.Should().Be(email);
        result.Data.FullName.Should().Be("Valid User");
        result.Data.Role.Should().Be(UserRole.Customer.ToString());

        result.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ShouldReturn400BadRequest_WhenUserDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new LoginRequest(
            "nonexisting@example.com",
            "Password123!");

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ShouldReturn400BadRequest_WhenPasswordIsIncorrect()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string email = "valid@example.com";
        const string correctPassword = "Password123!";

        var passwordHasher = Factory.Services
            .GetRequiredService<IPasswordHasher>();

        var passwordHash = passwordHasher.HashPassword(correctPassword);

        var user = new User(
            "Valid User",
            email,
            passwordHash,
            UserRole.Customer);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest(
            email,
            "WrongPassword123!");

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result =
            await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ShouldCreateRefreshToken_WhenCredentialsAreValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string email = "refresh@example.com";
        const string password = "Password123!";

        var passwordHasher = Factory.Services
            .GetRequiredService<IPasswordHasher>();

        var passwordHash = passwordHasher.HashPassword(password);

        var user = new User(
            "Refresh Token User",
            email,
            passwordHash,
            UserRole.Customer);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest(
            email,
            password);

        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<AuthResponse>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var refreshToken = result.Data!.RefreshToken;

        refreshToken.Should().NotBeNullOrWhiteSpace();

        var savedRefreshToken = DbContext.RefreshTokens
            .SingleOrDefault(token => token.Token == refreshToken);

        savedRefreshToken.Should().NotBeNull();
        savedRefreshToken!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Login_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        // Arrange
        await ResetDatabaseAsync();

        /*
         * LoginRateLimit:
         * 
         * PermitLimit = 5
         * Window = 1 minute
         *
         * We intentionally create a NEW factory for this test.
         * This prevents the rate-limit state from other tests
         * affecting this test.
         */
        using var factory = new SmartMarketWebApplicationFactory();

        var client = factory.CreateClient();

        var request = new LoginRequest("nonexisting@example.com", "WrongPassword123!");

        // Act
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 5; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", request);

            lastResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var exceededResponse = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        exceededResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.TooManyRequests);
    }
}
