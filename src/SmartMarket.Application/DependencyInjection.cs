using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Application.Common.Behaviors;
using SmartMarket.Application.Common.Security.Handlers;
using SmartMarket.Infrastructure.Security.Handlers;
using System.Reflection;

namespace SmartMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(assembly);
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddSingleton<IAuthorizationHandler, UserOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, StoreOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, CanCreateStoreHandler>();
        services.AddSingleton<IAuthorizationHandler, OrderOwnerOrAdminHandler>();
        services.AddSingleton<IAuthorizationHandler, SameAuthorOrStoreOwnerOrAdminHandler>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}