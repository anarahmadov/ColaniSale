using System.Text.Json;
using ColaniSale.Application.Authorization.Interfaces;
using StackExchange.Redis;

namespace ColaniSale.Infrastructure.Caching;

public sealed class RedisPermissionCache(IConnectionMultiplexer redis) : IPermissionCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<IReadOnlyCollection<string>?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(RedisCacheKeys.UserPermissions(userId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<string>>((string)value!, JsonOptions);
    }

    public async Task SetAsync(
        Guid userId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(permissions, JsonOptions);
        await _database.StringSetAsync(
            RedisCacheKeys.UserPermissions(userId),
            payload,
            TimeSpan.FromMinutes(30));
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(RedisCacheKeys.UserPermissions(userId));
}
