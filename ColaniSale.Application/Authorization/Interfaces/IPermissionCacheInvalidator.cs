namespace ColaniSale.Application.Authorization.Interfaces;

public interface IPermissionCacheInvalidator
{
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);
}
