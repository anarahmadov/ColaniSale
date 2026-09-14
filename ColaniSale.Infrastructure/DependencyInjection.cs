using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Infrastructure.Authorization;
using ColaniSale.Infrastructure.Caching;
using ColaniSale.Infrastructure.Data;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ColaniSale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (environment?.IsEnvironment("Testing") == true)
            {
                options.UseInMemoryDatabase("ColaniSaleTests");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        if (environment?.IsEnvironment("Testing") != true)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(
                    configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()?.ConnectionString
                    ?? "localhost:6379"));

            services.AddScoped<IPermissionCache, RedisPermissionCache>();
        }
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionCacheInvalidator, PermissionCacheInvalidator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<AuthorizationDataSeeder>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
