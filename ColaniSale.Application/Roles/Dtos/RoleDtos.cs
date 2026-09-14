namespace ColaniSale.Application.Roles.Dtos;

public sealed record CreateRoleRequest(string Name);

public sealed record UpdateRoleRequest(string Name);

public sealed record RoleResponse(Guid Id, string Name);

public sealed record AssignPermissionRequest(Guid PermissionId);

public sealed record PermissionResponse(Guid Id, string Name);
