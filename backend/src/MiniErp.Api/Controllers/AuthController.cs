using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Api.Security;
using MiniErp.Application.Common.Security;
using MiniErp.Application.Feature.Auth.Login;
using MiniErp.Application.Feature.Auth.Logout;
using MiniErp.Application.Feature.Auth.Refresh;
using MiniErp.Domain.Constants;

namespace MiniErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);
        
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResult>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue("sub"),
            email = User.FindFirstValue("email"),
            name = User.FindFirstValue("name"),
            roles = User.FindAll(AppClaims.Role).Select(c => c.Value)
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("pong");

    // [HttpGet("authz-debug")]
    // public async Task<IActionResult> AuthzDebug([FromServices] IAuthorizationService authorization)
    // {
    //     var result = await authorization.AuthorizeAsync(User, Policies.ManageUsers);

    //     return Ok(new
    //     {
    //         policySucceed = result.Succeeded,
    //         failedRequirements = result.Failure?.FailedRequirements.Select(r => r.ToString()),
    //         adminConstant = $"[{RoleNames.Admin}]",
    //         isInAdminContantRole = User.IsInRole(RoleNames.Admin)
    //     });
    // }
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);