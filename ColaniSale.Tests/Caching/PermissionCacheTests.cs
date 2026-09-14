using System.Net.Http.Headers;
using System.Net.Http.Json;
using ColaniSale.Application.Auth.Dtos;
using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Roles.Dtos;
using ColaniSale.Application.Users.Dtos;
using ColaniSale.Tests.Support;

namespace ColaniSale.Tests.Caching;

public sealed class PermissionCacheTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PermissionRequests_UseCacheAfterFirstLoad()
    {
        var cache = factory.GetPermissionCache();
        var token = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.GetAsync("/api/permissions/me");
        await _client.GetAsync("/api/permissions/me");

        Assert.Equal(2, cache.GetCallCount);
        Assert.Equal(1, cache.SetCallCount);
    }

    [Fact]
    public async Task RolePermissionChange_InvalidatesRoleUsersCache()
    {
        var cache = factory.GetPermissionCache();
        var adminToken = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createUserResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "seller.cache",
                "seller.cache@test.local",
                "Seller@12345",
                null));

        createUserResponse.EnsureSuccessStatusCode();

        var rolesResponse = await _client.GetAsync("/api/roles");
        rolesResponse.EnsureSuccessStatusCode();
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();
        var sellerRole = roles!.Single(role => role.Name == "Seller");

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListItem>>();
        var sellerUser = users!.Single(user => user.UserName == "seller.cache");

        await _client.PostAsJsonAsync(
            $"/api/users/{sellerUser.Id}/roles",
            new AssignRoleRequest(sellerRole.Id));

        _client.DefaultRequestHeaders.Authorization = null;
        var sellerToken = await LoginAsync("seller.cache", "Seller@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);

        await _client.GetAsync("/api/permissions/me");
        var removeCountBefore = cache.RemoveCallCount;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var allPermissionsResponse = await _client.GetAsync("/api/permissions");
        allPermissionsResponse.EnsureSuccessStatusCode();
        var allPermissions = await allPermissionsResponse.Content.ReadFromJsonAsync<List<PermissionResponse>>();

        var rolePermissionsResponse = await _client.GetAsync($"/api/roles/{sellerRole.Id}/permissions");
        rolePermissionsResponse.EnsureSuccessStatusCode();
        var rolePermissions = await rolePermissionsResponse.Content.ReadFromJsonAsync<List<PermissionResponse>>();
        var assignedIds = rolePermissions!.Select(permission => permission.Id).ToHashSet();

        var permissionToAssign = allPermissions!
            .First(permission => !assignedIds.Contains(permission.Id));

        await _client.PostAsJsonAsync(
            $"/api/roles/{sellerRole.Id}/permissions",
            new AssignPermissionRequest(permissionToAssign.Id));

        Assert.True(cache.RemoveCallCount > removeCountBefore);
    }

    [Fact]
    public async Task UserRoleChange_InvalidatesUserCache()
    {
        var cache = factory.GetPermissionCache();
        var adminToken = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createUserResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "role.cache",
                "role.cache@test.local",
                "Role@12345",
                null));

        createUserResponse.EnsureSuccessStatusCode();

        var rolesResponse = await _client.GetAsync("/api/roles");
        rolesResponse.EnsureSuccessStatusCode();
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();
        var sellerRole = roles!.Single(role => role.Name == "Seller");

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListItem>>();
        var user = users!.Single(item => item.UserName == "role.cache");

        var userToken = await LoginAsync("role.cache", "Role@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        await _client.GetAsync("/api/permissions/me");

        var removeCountBefore = cache.RemoveCallCount;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new AssignRoleRequest(sellerRole.Id));

        Assert.True(cache.RemoveCallCount > removeCountBefore);
    }

    [Fact]
    public async Task UserDeactivation_InvalidatesUserCache()
    {
        var cache = factory.GetPermissionCache();
        var adminToken = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createUserResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "cache.user",
                "cache.user@test.local",
                "Cache@12345",
                null));

        createUserResponse.EnsureSuccessStatusCode();

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListItem>>();
        var cacheUser = users!.Single(user => user.UserName == "cache.user");

        var userToken = await LoginAsync("cache.user", "Cache@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        await _client.GetAsync("/api/permissions/me");

        var removeCountBefore = cache.RemoveCallCount;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PutAsJsonAsync(
            $"/api/users/{cacheUser.Id}/active",
            new UpdateUserActiveRequest(false));

        Assert.True(cache.RemoveCallCount > removeCountBefore);
    }

    private async Task<string> LoginAsync(string userName, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(userName, password));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }

    private sealed record UserListItem(
        Guid Id,
        string UserName,
        string? Email,
        bool IsActive,
        IReadOnlyCollection<string>? Roles);
}
