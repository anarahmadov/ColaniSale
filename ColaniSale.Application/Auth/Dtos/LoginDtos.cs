namespace ColaniSale.Application.Auth.Dtos;

public sealed record LoginRequest(string UserName, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt);
