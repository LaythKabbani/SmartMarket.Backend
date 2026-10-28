using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Dtos;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Subscriptions;

public class CheckSubscriptionByStoreIdEndpointTests : ApiTestBase
{
    private readonly JsonSerializerOptions _jsonOptions;

    public CheckSubscriptionByStoreIdEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public async Task CheckSubscription_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckSubscription_ShouldReturn404NotFound_WhenSubscriptionDoesNotExist()
    {
        await ResetDatabaseAsync();

        var user = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckSubscription_ShouldReturn200OK_WhenSubscriptionExists()
    {
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", merchant.Id);
        DbContext.Stores.Add(store);

        var subscriptionPlan = new SubscriptionPlanConfig(SubscriptionPlan.Monthly, 100.0m, "USD");
        DbContext.SubscriptionPlans.Add(subscriptionPlan);
        await DbContext.SaveChangesAsync();

        var subscription = new MerchantSubscription(
            store.Id,
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(25),
            SubscriptionStatus.Active
        );
        DbContext.MerchantSubscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync($"/api/subscriptions/{store.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<SubscriptionDto>>(_jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.StoreId.Should().Be(store.Id);
    }

    [Fact]
    public async Task CheckSubscription_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var factory = new SmartMarketWebApplicationFactory();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}