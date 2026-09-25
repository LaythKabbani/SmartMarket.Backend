using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Features.Auth.Commands.ResetPassword;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Auth;

public class ResetPasswordEndpointTests : ApiTestBase
{
    public ResetPasswordEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn200OK_WhenOtpAndNewPasswordAreValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        const string email = "reset@example.com";
        const string validOtp = "123456";

        var user = new User("Reset User", email, "OldHashedPassword123", UserRole.Customer);
        user.SetPasswordResetOtp(validOtp, DateTime.UtcNow.AddMinutes(10));
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(email, validOtp, "NewPassword123!");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400BadRequest_WhenOtpIsInvalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var command = new ResetPasswordCommand("user@example.com", "000000", "NewPassword123!");
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        // Arrange
        await ResetDatabaseAsync();

        using var factory = new SmartMarketWebApplicationFactory();
        var client = factory.CreateClient();

        var command = new ResetPasswordCommand("test@example.com", "123456", "NewPassword123!");

        // Act
        for (var i = 0; i < 3; i++)
        {
            var res = await client.PostAsJsonAsync("/api/auth/reset-password", command);
            res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }

        var exceededResponse = await client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        exceededResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}