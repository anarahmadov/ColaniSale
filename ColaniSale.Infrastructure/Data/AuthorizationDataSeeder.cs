using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Authorization.Roles;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ColaniSale.Infrastructure.Data;

public sealed class AuthorizationDataSeeder(
    AppDbContext dbContext,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminOptions> adminOptions,
    ILogger<AuthorizationDataSeeder> logger)
{
    private static readonly IReadOnlyDictionary<string, string[]> RolePermissionMatrix =
        new Dictionary<string, string[]>
        {
            [AppRoles.Admin] =
            [
                Permissions.User.View,
                Permissions.User.Create,
                Permissions.User.Update,
                Permissions.User.Delete,
                Permissions.Product.View,
                Permissions.Product.Create,
                Permissions.Product.Update,
                Permissions.Product.Delete,
                Permissions.Customer.View,
                Permissions.Customer.Create,
                Permissions.Customer.Update,
                Permissions.Customer.Delete,
                Permissions.Sale.View,
                Permissions.Sale.Create,
                Permissions.Sale.Update,
                Permissions.Sale.Delete,
                Permissions.Payment.View,
                Permissions.Payment.Create,
                Permissions.Payment.Update,
                Permissions.Payment.Delete,
                Permissions.Report.View
            ],
            [AppRoles.Manager] =
            [
                Permissions.Sale.View,
                Permissions.Sale.Create,
                Permissions.Sale.Update,
                Permissions.Customer.View,
                Permissions.Customer.Update,
                Permissions.Product.View,
                Permissions.Payment.View,
                Permissions.Report.View
            ],
            [AppRoles.Seller] =
            [
                Permissions.Sale.View,
                Permissions.Sale.Create,
                Permissions.Customer.View
            ],
            [AppRoles.Warehouse] =
            [
                Permissions.Product.View,
                Permissions.Product.Create,
                Permissions.Product.Update,
                Permissions.Product.Delete,
                Permissions.Sale.View
            ],
            [AppRoles.Accountant] =
            [
                Permissions.Payment.View,
                Permissions.Payment.Create,
                Permissions.Payment.Update,
                Permissions.Payment.Delete,
                Permissions.Sale.View,
                Permissions.Customer.View,
                Permissions.Report.View
            ]
        };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingPermissions = await dbContext.Permissions
            .AsNoTracking()
            .Select(permission => permission.Name)
            .ToListAsync(cancellationToken);

        var permissionsToAdd = Permissions.GetAll()
            .Except(existingPermissions, StringComparer.Ordinal)
            .Select(name => new Domain.Entities.Permission
            {
                Id = Guid.NewGuid(),
                Name = name
            })
            .ToList();

        if (permissionsToAdd.Count == 0)
        {
            return;
        }

        dbContext.Permissions.AddRange(permissionsToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in AppRoles.GetAll())
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create role {RoleName}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var permissionsByName = await dbContext.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(permission => permission.Name, cancellationToken);

        var rolesByName = await dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(role => role.Name!, cancellationToken);

        var existingRolePermissions = await dbContext.RolePermissions
            .AsNoTracking()
            .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .ToListAsync(cancellationToken);

        var existingKeys = existingRolePermissions
            .Select(item => (item.RoleId, item.PermissionId))
            .ToHashSet();

        var rolePermissionsToAdd = new List<Domain.Entities.RolePermission>();

        foreach (var (roleName, permissionNames) in RolePermissionMatrix)
        {
            if (!rolesByName.TryGetValue(roleName, out var role))
            {
                continue;
            }

            foreach (var permissionName in permissionNames)
            {
                if (!permissionsByName.TryGetValue(permissionName, out var permission))
                {
                    continue;
                }

                if (existingKeys.Contains((role.Id, permission.Id)))
                {
                    continue;
                }

                rolePermissionsToAdd.Add(new Domain.Entities.RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        if (rolesByName.TryGetValue(AppRoles.SuperAdmin, out var superAdminRole))
        {
            foreach (var permission in permissionsByName.Values)
            {
                if (existingKeys.Contains((superAdminRole.Id, permission.Id)))
                {
                    continue;
                }

                rolePermissionsToAdd.Add(new Domain.Entities.RolePermission
                {
                    RoleId = superAdminRole.Id,
                    PermissionId = permission.Id
                });
            }
        }

        if (rolePermissionsToAdd.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(rolePermissionsToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var adminSettings = adminOptions.Value;
        if (string.IsNullOrWhiteSpace(adminSettings.Email)
            || string.IsNullOrWhiteSpace(adminSettings.Password))
        {
            logger.LogWarning("Admin credentials are not configured. Skipping SuperAdmin user seeding.");
            return;
        }

        var existingUser = await userManager.FindByEmailAsync(adminSettings.Email);
        if (existingUser is not null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, AppRoles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existingUser, AppRoles.SuperAdmin);
            }

            return;
        }

        var superAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminSettings.Email,
            Email = adminSettings.Email,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(superAdmin, adminSettings.Password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to create SuperAdmin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(error => error.Description)));
            return;
        }

        await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
    }
}
