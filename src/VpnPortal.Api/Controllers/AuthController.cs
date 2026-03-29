using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Contracts.Auth;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IAntiforgery antiforgery) : ControllerBase
{
    [IgnoreAntiforgeryToken]
    [HttpPost("login")]
    [ProducesResponseType<SessionUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await authService.AuthenticateUserAsync(command, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await SignInAsync(user);
        antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(user);
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("admin/login")]
    [ProducesResponseType<SessionUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminLogin([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await authService.AuthenticateSuperAdminAsync(command, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await SignInAsync(user);
        antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<SessionUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var login = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (!int.TryParse(idClaim, out var id) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new SessionUserDto(id, login, role, email));
    }

    private async Task SignInAsync(SessionUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}
