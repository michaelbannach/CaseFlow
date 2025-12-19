using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;


using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmployeeRepository employeeRepo,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _employeeRepo = employeeRepo;
        _config = config;
    }

    public async Task<(bool success, string? error)> RegisterAsync(RegisterRequest request)
    {
        // Basic validation (API-level, nicht Domain-level)
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Email and password are required");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        // Employee anlegen (fachliche Identität)
        var employee = new Employee
        {
            ApplicationUserId = user.Id,
            Name = request.Name?.Trim() ?? string.Empty,
            Role = request.Role,
            DepartmentId = request.DepartmentId
        };

        var added = await _employeeRepo.AddAsync(employee);
        if (!added)
        {
            // Konsistenz: wenn Employee nicht angelegt werden kann, Identity-User wieder entfernen
            await _userManager.DeleteAsync(user);
            return (false, "Could not create employee");
        }

        return (true, null);
    }

    public async Task<(bool success, string? error, string? token)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Invalid credentials", null);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return (false, "Invalid credentials", null);

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
            return (false, "Invalid credentials", null);

        var employee = await _employeeRepo.GetByApplicationUserIdAsync(user.Id);
        if (employee == null)
            return (false, "Employee not found", null);

        var token = CreateJwt(user, employee);
        return (true, null, token);
    }

    private string CreateJwt(ApplicationUser user, Employee employee)
    {
        var jwt = _config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 60;

        // Claims: sub=userId, plus employeeId + role
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),

            new("employeeId", employee.Id.ToString()),
            new(ClaimTypes.Role, employee.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
