using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Infrastructure.Persistence;
using System.Text.Json.Serialization;

namespace SmartMarket.Api.IntegrationTests.Infrastructure;

public class SmartMarketWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"SmartMarketIntegrationTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the PostgreSQL ApplicationDbContext
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add an isolated in-memory database for this test factory
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
