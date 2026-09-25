using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Users.Commands.CreateUser;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Users;

public class CreateUserEndpointTests : ApiTestBase
{
    public CreateUserEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateUser_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var command = new CreateUserCommand("newuser@example.com", "Password123!", "New User", UserRole.Customer);

        // Act
        var response = await client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn403Forbidden_WhenUserIsNotSuperAdmin()
    {
        // Arrange
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new CreateUserCommand("created@example.com", "Password123!", "Created User", UserRole.Customer);

        // Act
        var response = await client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn200OK_WhenUserIsSuperAdminAndDataIsValid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var admin = new User("Admin User", "admin@example.com", "Password123!", UserRole.SuperAdmin);
        DbContext.Users.Add(admin);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(admin);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new CreateUserCommand("New Created User", "newcreated@example.com", "Password123!", UserRole.Merchant);

        // Act
        var response = await client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }
}