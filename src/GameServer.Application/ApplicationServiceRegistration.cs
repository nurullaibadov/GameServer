using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Explicit assembly to avoid ambiguous call
        services.AddAutoMapper(new[] { typeof(ApplicationServiceRegistration).Assembly });
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        return services;
    }
}
