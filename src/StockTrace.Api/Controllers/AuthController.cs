using StockTrace.Api.Auth;
using StockTrace.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ITokenService tokenService, ITestUserStore testUserStore) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResult> Login(LoginRequest request)
    {
        var user = testUserStore.Find(request.Username, request.Password);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid username or password."
            });
        }

        return Ok(tokenService.CreateToken(user));
    }

    [HttpGet("me")]
    [ProducesResponseType<AuthenticatedUserResult>(StatusCodes.Status200OK)]
    public ActionResult<AuthenticatedUserResult> Me()
    {
        var username = User.Identity?.Name ?? string.Empty;
        var displayName = User.FindFirst("display_name")?.Value ?? username;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var permissions = User.FindAll(AppClaimTypes.Permission).Select(x => x.Value).ToArray();
        return Ok(new AuthenticatedUserResult(username, displayName, role, permissions));
    }
}
