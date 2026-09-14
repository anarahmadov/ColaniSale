namespace ColaniSale.Application.Authorization.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyCollection<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
