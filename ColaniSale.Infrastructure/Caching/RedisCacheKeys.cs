namespace ColaniSale.Infrastructure.Caching;

public static class RedisCacheKeys
{
    public static string UserPermissions(Guid userId) => $"permissions:user:{userId}";
}
