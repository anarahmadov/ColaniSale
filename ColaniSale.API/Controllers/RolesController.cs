using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Roles.Dtos;
using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Infrastructure;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColaniSale.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolesController(
    RoleManager<ApplicationRole> roleManager,
    AppDbContext dbContext,
    IPermissionCacheInvalidator cacheInvalidator) : ControllerBase
{
    [Authorize(Policy = Permissions.User.View)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleResponse(role.Id, role.Name!))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [Authorize(Policy = Permissions.User.Create)]
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant()
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetRoles), new RoleResponse(role.Id, role.Name!));
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpperInvariant();

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await cacheInvalidator.InvalidateRoleUsersAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.User.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        await cacheInvalidator.InvalidateRoleUsersAsync(id, cancellationToken);

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [Authorize(Policy = Permissions.User.View)]
    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> GetRolePermissions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        var permissions = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == id)
            .Select(rolePermission => new PermissionResponse(
                rolePermission.Permission.Id,
                rolePermission.Permission.Name))
            .OrderBy(permission => permission.Name)
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermission(
        Guid id,
        [FromBody] AssignPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        var permissionExists = await dbContext.Permissions
            .AsNoTracking()
            .AnyAsync(permission => permission.Id == request.PermissionId, cancellationToken);

        if (!permissionExists)
        {
            return NotFound("Permission not found.");
        }

        var alreadyAssigned = await dbContext.RolePermissions
            .AnyAsync(
                rolePermission => rolePermission.RoleId == id
                    && rolePermission.PermissionId == request.PermissionId,
                cancellationToken);

        if (alreadyAssigned)
        {
            return NoContent();
        }

        dbContext.RolePermissions.Add(new Domain.Entities.RolePermission
        {
            RoleId = id,
            PermissionId = request.PermissionId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateRoleUsersAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(
        Guid id,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var rolePermission = await dbContext.RolePermissions
            .FirstOrDefaultAsync(
                item => item.RoleId == id && item.PermissionId == permissionId,
                cancellationToken);

        if (rolePermission is null)
        {
            return NotFound();
        }

        dbContext.RolePermissions.Remove(rolePermission);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateRoleUsersAsync(id, cancellationToken);
        return NoContent();
    }
}
