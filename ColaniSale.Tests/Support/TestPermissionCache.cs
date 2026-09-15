using System.Collections.Concurrent;
using ColaniSale.Application.Authorization.Interfaces;

namespace ColaniSale.Tests.Support;

public sealed class TestPermissionCache : IPermissionCache
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<string>> _cache = new();

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public Task<IReadOnlyCollection<string>?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        _cache.TryGetValue(userId, out var permissions);
        return Task.FromResult(permissions);
    }

    public Task SetAsync(
        Guid userId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        SetCallCount++;
        _cache[userId] = permissions.ToList();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RemoveCallCount++;
        _cache.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
