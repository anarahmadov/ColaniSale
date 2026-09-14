using ColaniSale.Application.Auth.Dtos;
using ColaniSale.Application.Authorization.Interfaces;
using ColaniSale.Application.Authorization.Permissions;
using ColaniSale.Application.Roles.Dtos;
using ColaniSale.Infrastructure;
using ColaniSale.Infrastructure.Data;
using ColaniSale.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColaniSale.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName)
            ?? await userManager.FindByEmailAsync(request.UserName);

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return Unauthorized();
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user, out var expiresAt);
        return Ok(new LoginResponse(accessToken, expiresAt));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PermissionsController(
    IPermissionService permissionService,
    AppDbContext dbContext) : ControllerBase
{
    [Authorize(Policy = Permissions.User.View)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> GetPermissions(
        CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => new PermissionResponse(permission.Id, permission.Name))
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetMyPermissions(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var permissions = await permissionService.GetUserPermissionsAsync(userId, cancellationToken);
        return Ok(permissions);
    }
}
