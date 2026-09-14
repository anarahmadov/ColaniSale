namespace ColaniSale.Application.Users.Dtos;

public sealed record AssignRoleRequest(Guid RoleId);

public sealed record UserResponse(
    Guid Id,
    string UserName,
    string? Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    IReadOnlyCollection<Guid>? RoleIds);

public sealed record UpdateUserActiveRequest(bool IsActive);
