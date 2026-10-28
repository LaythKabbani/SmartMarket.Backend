using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.UpdatePlanPrice;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Subscriptions;

public class UpdatePlanPriceEndpointTests : ApiTestBase
{
    public UpdatePlanPriceEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdatePlanPrice_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();
        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);

        var response = await client.PutAsJsonAsync("/api/subscriptions/plans/price", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePlanPrice_ShouldReturn403Forbidden_WhenUserIsMerchant()
    {
        await ResetDatabaseAsync();

        var merchant = new User("Merchant User", "merchant@test.com", "Pass123!", UserRole.Merchant);
        DbContext.Users.Add(merchant);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(merchant);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);

        var response = await client.PutAsJsonAsync("/api/subscriptions/plans/price", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePlanPrice_ShouldReturn200OK_WhenSuperAdminRequestIsValid()
    {
        await ResetDatabaseAsync();

        var superAdmin = new User("Admin User", "admin@test.com", "Pass123!", UserRole.SuperAdmin);
        DbContext.Users.Add(superAdmin);

        var subscriptionPlan = new SubscriptionPlanConfig(SubscriptionPlan.Monthly, 100.0m, "USD");
        DbContext.SubscriptionPlans.Add(subscriptionPlan);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(superAdmin);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);

        var response = await client.PutAsJsonAsync("/api/subscriptions/plans/price", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePlanPrice_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        await ResetDatabaseAsync();

        var superAdmin = new User("Admin User", "admin@test.com", "Pass123!", UserRole.SuperAdmin);
        DbContext.Users.Add(superAdmin);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(superAdmin);

        var factory = new SmartMarketWebApplicationFactory();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);
        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.PutAsJsonAsync("/api/subscriptions/plans/price", command);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}