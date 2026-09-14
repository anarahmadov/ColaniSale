using ColaniSale.Application.Authorization.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ColaniSale.Infrastructure.Authorization;

public sealed class PermissionService(
    AppDbContext dbContext,
    IPermissionCache permissionCache) : IPermissionService
{
    public async Task<IReadOnlyCollection<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cachedPermissions = await permissionCache.GetAsync(userId, cancellationToken);
        if (cachedPermissions is not null)
        {
            return cachedPermissions;
        }

        var permissions = await (
            from user in dbContext.Users.AsNoTracking()
            where user.Id == userId && user.IsActive
            from userRole in dbContext.UserRoles.AsNoTracking()
            where userRole.UserId == user.Id
            join rolePermission in dbContext.RolePermissions.AsNoTracking()
                on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            select permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissions.Count > 0)
        {
            await permissionCache.SetAsync(userId, permissions, cancellationToken);
        }

        return permissions;
    }
}
