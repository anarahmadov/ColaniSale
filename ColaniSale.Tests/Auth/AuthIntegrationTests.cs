using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ColaniSale.Application.Auth.Dtos;
using ColaniSale.Tests.Support;

namespace ColaniSale.Tests.Auth;

public sealed class AuthIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@colanisale.local", "Admin@12345"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(payload?.AccessToken));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@colanisale.local", "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var token = await LoginAsync("admin@colanisale.local", "Admin@12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();

        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListItem>>();
        var adminUser = users!.Single(user => user.Email == "admin@colanisale.local");

        var deactivateResponse = await _client.PutAsJsonAsync(
            $"/api/users/{adminUser.Id}/active",
            new { IsActive = false });

        deactivateResponse.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@colanisale.local", "Admin@12345"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PutAsJsonAsync(
            $"/api/users/{adminUser.Id}/active",
            new { IsActive = true });
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

    private sealed record UserListItem(Guid Id, string UserName, string? Email, bool IsActive);
}
