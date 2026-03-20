using GameServer.Application.Interfaces;
using GameServer.Domain.Interfaces;
using GameServer.Infrastructure.Data;
using GameServer.Infrastructure.Repositories;
using GameServer.Infrastructure.Services;
using GameServer.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("GameServer.Infrastructure")));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
