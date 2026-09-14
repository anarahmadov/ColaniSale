using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Users.Dtos;
using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColaniSale.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IPermissionCacheInvalidator cacheInvalidator) : ControllerBase
{
    [Authorize(Policy = Permissions.User.View)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        var responses = new List<UserResponse>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            responses.Add(new UserResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email,
                user.IsActive,
                roles.ToList()));
        }

        return Ok(responses);
    }

    [Authorize(Policy = Permissions.User.Create)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        if (request.RoleIds is not null)
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleManager.FindByIdAsync(roleId.ToString());
                if (role is null)
                {
                    continue;
                }

                await userManager.AddToRoleAsync(user, role.Name!);
            }
        }

        var assignedRoles = await userManager.GetRolesAsync(user);
        return CreatedAtAction(
            nameof(GetUsers),
            new UserResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email,
                user.IsActive,
                assignedRoles.ToList()));
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var role = await roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null)
        {
            return NotFound("Role not found.");
        }

        if (await userManager.IsInRoleAsync(user, role.Name!))
        {
            return NoContent();
        }

        var result = await userManager.AddToRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await cacheInvalidator.InvalidateUserAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(
        Guid id,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return NotFound("Role not found.");
        }

        if (!await userManager.IsInRoleAsync(user, role.Name!))
        {
            return NotFound();
        }

        var result = await userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await cacheInvalidator.InvalidateUserAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Permissions.User.Update)]
    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActiveState(
        Guid id,
        [FromBody] UpdateUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = request.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await cacheInvalidator.InvalidateUserAsync(id, cancellationToken);
        return NoContent();
    }
}
