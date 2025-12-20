using CaseFlow.Application.Contracts.Auth;

namespace CaseFlow.Application.Interfaces;

public interface IAuthService
{
    Task<(bool success, string? error)> RegisterAsync(RegisterRequest request);
    Task<(bool success, string? error, string? token)> LoginAsync(LoginRequest request);
    
    Task LogoutAsync();
}
