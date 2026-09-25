using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Api.IntegrationTests.Infrastructure;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Features.Orders;

public class CancelOrderEndpointTests : ApiTestBase
{
    public CancelOrderEndpointTests(SmartMarketWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Cancel_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();

        var client = CreateClient();

        // Act
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancel_ShouldReturn200OK_WhenOrderCanBeCancelled()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Customer User", "customer_cancel@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(user);

        var order = new Order(user.Id, 80.0m, "Cancel Address");
        DbContext.Orders.Add(order);

        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_ShouldReturn400BadRequest_WhenOrderDoesNotExistOrCannotBeCancelled()
    {
        // Arrange
        await ResetDatabaseAsync();

        var user = new User("Customer User", "customer_cancel2@example.com", "Password123!", UserRole.Customer);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var jwtProvider = Factory.Services.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtProvider.GenerateAccessToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ResultDto<bool>>();

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }
}