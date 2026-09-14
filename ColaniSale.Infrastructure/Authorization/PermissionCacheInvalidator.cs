using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ColaniSale.Infrastructure.Authorization;

public sealed class PermissionCacheInvalidator(
    AppDbContext dbContext,
    IPermissionCache permissionCache) : IPermissionCacheInvalidator
{
    public Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        permissionCache.RemoveAsync(userId, cancellationToken);

    public async Task InvalidateRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var userIds = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.RoleId == roleId)
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await permissionCache.RemoveAsync(userId, cancellationToken);
        }
    }
}
