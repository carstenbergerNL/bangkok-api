using Bangkok.Application.Configuration;
using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Data;
using Bangkok.Infrastructure.Repositories;
using Bangkok.Infrastructure.Security;
using Bangkok.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bangkok.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqlConnectionOptions>(options =>
        {
            options.DefaultConnection = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        });
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IAuditLogger, AuditLogger>();

        return services;
    }
}
