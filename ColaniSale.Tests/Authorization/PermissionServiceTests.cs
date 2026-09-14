using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Authorization.Roles;
using ColaniSale.Domain.Entities;
using ColaniSale.Infrastructure;
using ColaniSale.Infrastructure.Authorization;
using ColaniSale.Infrastructure.Identity;
using ColaniSale.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColaniSale.Tests.Authorization;

public sealed class PermissionServiceTests
{
    [Fact]
    public async Task GetUserPermissionsAsync_UsesCacheOnSecondRequest()
    {
        var provider = await CreateServiceProviderAsync();
        var cache = provider.GetRequiredService<TestPermissionCache>();
        var permissionService = provider.GetRequiredService<PermissionService>();
        var userId = await SeedSellerUserAsync(provider);

        await permissionService.GetUserPermissionsAsync(userId);
        await permissionService.GetUserPermissionsAsync(userId);

        Assert.Equal(2, cache.GetCallCount);
        Assert.Equal(1, cache.SetCallCount);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsPermissionsThroughRoleHierarchy()
    {
        var provider = await CreateServiceProviderAsync();
        var permissionService = provider.GetRequiredService<PermissionService>();
        var userId = await SeedSellerUserAsync(provider);

        var permissions = await permissionService.GetUserPermissionsAsync(userId);

        Assert.Contains(Permissions.Sale.View, permissions);
        Assert.Contains(Permissions.Sale.Create, permissions);
        Assert.Contains(Permissions.Customer.View, permissions);
        Assert.DoesNotContain(Permissions.User.Delete, permissions);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_InactiveUser_ReturnsEmptyPermissions()
    {
        var provider = await CreateServiceProviderAsync();
        var permissionService = provider.GetRequiredService<PermissionService>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var userId = await SeedSellerUserAsync(provider);

        var user = await userManager.FindByIdAsync(userId.ToString());
        user!.IsActive = false;
        await userManager.UpdateAsync(user);

        var permissions = await permissionService.GetUserPermissionsAsync(userId);

        Assert.Empty(permissions);
    }

    private static async Task<IServiceProvider> CreateServiceProviderAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddSingleton<TestPermissionCache>();
        services.AddSingleton<IPermissionCache>(provider =>
            provider.GetRequiredService<TestPermissionCache>());
        services.AddScoped<PermissionService>();

        var provider = services.BuildServiceProvider();
        await SeedAuthorizationDataAsync(provider);
        return provider;
    }

    private static async Task SeedAuthorizationDataAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var permissionName in Permissions.GetAll())
        {
            dbContext.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Name = permissionName
            });
        }

        await dbContext.SaveChangesAsync();

        var sellerRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = AppRoles.Seller,
            NormalizedName = AppRoles.Seller.ToUpperInvariant()
        };

        await roleManager.CreateAsync(sellerRole);

        var sellerPermissions = new[]
        {
            Permissions.Sale.View,
            Permissions.Sale.Create,
            Permissions.Customer.View
        };

        var permissions = await dbContext.Permissions
            .Where(permission => sellerPermissions.Contains(permission.Name))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = sellerRole.Id,
                PermissionId = permission.Id
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> SeedSellerUserAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "seller@test.local",
            Email = "seller@test.local",
            EmailConfirmed = true,
            IsActive = true
        };

        await userManager.CreateAsync(user, "Seller@12345");
        await userManager.AddToRoleAsync(user, (await roleManager.FindByNameAsync(AppRoles.Seller))!.Name!);
        return user.Id;
    }
}
