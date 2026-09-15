namespace ColaniSale.Application.Authorization.Interfaces;

public interface IPermissionCache
{
    Task<IReadOnlyCollection<string>?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid userId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
