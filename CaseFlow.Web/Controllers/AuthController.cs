using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;


namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error) = await _auth.RegisterAsync(request);
        if (!success)
            return BadRequest(new { error });

        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, token) = await _auth.LoginAsync(request);
        if (!success)
            return Unauthorized(new { error });

        return Ok(new { token });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Bei purem JWT: Server kann ein bereits ausgestelltes Token nicht "zurückholen".
        // Üblicher Logout = Client löscht Token.
        await _auth.LogoutAsync(); // optional (siehe Hinweis unten)

        return Ok(new
        {
            message = "Logged out. Please delete the JWT on the client."
        });
    }
}