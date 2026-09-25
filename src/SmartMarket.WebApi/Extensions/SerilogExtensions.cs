using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace SmartMarket.WebApi.Extensions;

public static class SerilogExtensions
{
    public static void ConfigureSerilog(this ConfigureHostBuilder host)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("logs/smartmarketx-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        host.UseSerilog();
    }
}