using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.WebApi.Controllers;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Stores;

public class CreateStoreEndpointTests : ApiTestBase
{
    public CreateStoreEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateStore_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();
        var client = CreateClient();
        var request = new CreateStoreRequest("New Store", "Description", "https://logo.com/logo.png");

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStore_ShouldReturn403Forbidden_WhenUserIsCustomer()
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

        var request = new CreateStoreRequest("Forbidden Store", "Desc", "https://logo.com/logo.png");

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateStore_ShouldReturn200OK_WhenUserIsMerchantAndRequestIsValid()
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

        var request = new CreateStoreRequest("Merchant Super Store", "All items available", "https://logo.com/store.png");

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateStore_ShouldReturn200OK_WhenUserIsSuperAdminAndRequestIsValid()
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

        var request = new CreateStoreRequest("Admin Official Store", "Official store", null);

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateStore_ShouldReturn400BadRequest_WhenDataIsInvalid()
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

        var request = new CreateStoreRequest("", "Invalid Store Data", null);

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateStore_ShouldReturn400BadRequest_WhenMerchantHadStore()
    {
        // Arrange
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@example.com", "Password123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Existing Store", merchant.Id, "Already has a store", null);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateStoreRequest("Merchant Super Store", "All items available", "https://logo.com/store.png");

        // Act
        var response = await client.PostAsJsonAsync("/api/stores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}