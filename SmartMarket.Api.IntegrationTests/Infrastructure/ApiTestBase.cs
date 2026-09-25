using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMarket.Api.IntegrationTests.Infrastructure;

public abstract class ApiTestBase : IClassFixture<SmartMarketWebApplicationFactory>
{
    protected readonly SmartMarketWebApplicationFactory Factory;
    protected readonly ApplicationDbContext DbContext;

    protected ApiTestBase(SmartMarketWebApplicationFactory factory)
    {
        Factory = factory;

        var scope = Factory.Services.CreateScope();

        DbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
    }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected HttpClient CreateClient()
    {
        return Factory.CreateClient();
    }

    protected async Task ResetDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }
}
