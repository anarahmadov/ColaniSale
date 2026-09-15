using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ColaniSale.Tests.Support;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveService<IConnectionMultiplexer>(services);
            RemoveService<IPermissionCache>(services);

            services.AddSingleton<TestPermissionCache>();
            services.AddSingleton<IPermissionCache>(provider =>
                provider.GetRequiredService<TestPermissionCache>());
        });
    }

    public TestPermissionCache GetPermissionCache() =>
        Services.GetRequiredService<TestPermissionCache>();

    private static void RemoveService<TService>(IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
