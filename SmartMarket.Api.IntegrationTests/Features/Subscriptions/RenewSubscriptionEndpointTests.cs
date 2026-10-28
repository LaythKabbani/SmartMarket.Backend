using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Subscriptions;

public class RenewSubscriptionEndpointTests : ApiTestBase
{
    public RenewSubscriptionEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RenewSubscription_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();
        var command = new RenewSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

        var response = await client.PostAsJsonAsync("/api/subscriptions/renew", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RenewSubscription_ShouldReturn403Forbidden_WhenUserIsCustomer()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new RenewSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

        var response = await client.PostAsJsonAsync("/api/subscriptions/renew", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RenewSubscription_ShouldReturn200OK_WhenMerchantRequestIsValid()
    {
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", merchant.Id);
        DbContext.Stores.Add(store);

        var subscription = new MerchantSubscription(
            store.Id,
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            SubscriptionStatus.Expired
        );
        DbContext.MerchantSubscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Demo, null);

        var response = await client.PostAsJsonAsync("/api/subscriptions/renew", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RenewSubscription_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
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

        var command = new RenewSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Demo, null);
        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/subscriptions/renew", command);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}