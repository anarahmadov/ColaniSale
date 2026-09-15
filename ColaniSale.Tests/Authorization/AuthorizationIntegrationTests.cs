using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ColaniSale.Application.Auth.Dtos;
using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Users.Dtos;
using ColaniSale.Tests.Support;

namespace ColaniSale.Tests.Authorization;

public sealed class AuthorizationIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithPermission_ReturnsOk()
    {
        var token = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutPermission_ReturnsForbidden()
    {
        var adminToken = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createUserResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "limited.user",
                "limited.user@test.local",
                "Limited@12345",
                null));

        createUserResponse.EnsureSuccessStatusCode();

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserResponse>>();
        var limitedUser = users!.Single(user => user.UserName == "limited.user");

        _client.DefaultRequestHeaders.Authorization = null;
        var limitedToken = await LoginAsync("limited.user", "Limited@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", limitedToken);

        var forbiddenResponse = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task PermissionEndpoint_ReturnsAssignedPermissions()
    {
        var token = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/permissions/me");
        response.EnsureSuccessStatusCode();

        var permissions = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(permissions);
        Assert.Contains(Permissions.User.View, permissions);
        Assert.Contains(Permissions.Sale.Create, permissions);
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
}
