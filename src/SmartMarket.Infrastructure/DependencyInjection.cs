using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Security.Handlers;
using SmartMarket.Infrastructure.Authentication;
using SmartMarket.Infrastructure.Persistence;
using SmartMarket.Infrastructure.Security.Handlers;
using SmartMarket.Infrastructure.Services;

namespace SmartMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IAuthorizationHandler, UserOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, StoreOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, CanCreateStoreHandler>();
        services.AddSingleton<IAuthorizationHandler, OrderOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, SameAuthorOrStoreOwnerOrAdminHandler>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Later here I will add repositories and services like JWT, Email, FileStorage.

        return services;
    }
}