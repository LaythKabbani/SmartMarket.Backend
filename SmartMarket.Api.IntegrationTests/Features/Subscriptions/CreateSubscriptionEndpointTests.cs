using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Subscriptions;

public class CreateSubscriptionEndpointTests : ApiTestBase
{
    public CreateSubscriptionEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateSubscription_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();
        var command = new CreateSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

        var response = await client.PostAsJsonAsync("/api/subscriptions", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSubscription_ShouldReturn200OK_WhenDemoPlanIsValid()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var store = new Store("Test Store", customer.Id);
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Demo, null);

        var response = await client.PostAsJsonAsync("/api/subscriptions", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultDto<Guid>>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSubscription_ShouldReturn429TooManyRequests_WhenRateLimitIsExceeded()
    {
        await ResetDatabaseAsync();

        var customer = new User("Customer User", "customer@test.com", "Pass123!", UserRole.Customer);
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(customer);

        var factory = new SmartMarketWebApplicationFactory();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var command = new CreateSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Demo, null);

        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/subscriptions", command);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}