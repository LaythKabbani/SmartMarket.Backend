using Microsoft.Extensions.DependencyInjection;

namespace SmartMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // مستقبلاً هون رح نسجل الـ MediatR, AutoMapper, FluentValidation
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}