using CaseFlow.Application.Contracts.Auth;
using CaseFlow.Application.Interfaces;
using CaseFlow.Web.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var request = new RegisterRequest
        {
            Email = dto.Email,
            Password = dto.Password,
            Name = dto.Name,
            Role = dto.Role,
            DepartmentId = dto.DepartmentId
        };

        var (success, error) = await _auth.RegisterAsync(request);
        if (!success) return BadRequest(new { error });

        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var request = new LoginRequest
        {
            Email = dto.Email,
            Password = dto.Password
        };

        var (success, error, token) = await _auth.LoginAsync(request);
        if (!success) return Unauthorized(new { error });

        return Ok(new { token });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync();
        return Ok(new { message = "Logged out. Please delete the JWT on the client." });
    }
}