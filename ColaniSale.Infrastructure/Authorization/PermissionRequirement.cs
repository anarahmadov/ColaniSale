using Microsoft.AspNetCore.Authorization;

namespace ColaniSale.Infrastructure.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
