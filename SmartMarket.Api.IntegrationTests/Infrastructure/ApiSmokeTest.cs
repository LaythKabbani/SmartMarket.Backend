using System.Net;
using FluentAssertions;
using Xunit;

namespace SmartMarket.Api.IntegrationTests.Infrastructure;

public class ApiSmokeTests
    : IClassFixture<SmartMarketWebApplicationFactory>
{
    private readonly SmartMarketWebApplicationFactory _factory;

    public ApiSmokeTests(SmartMarketWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_ShouldStart()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/login");

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
