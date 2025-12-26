using System.Security.Claims;

namespace CaseFlow.Web.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("employeeId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("EmployeeId claim is missing.");

        if (!int.TryParse(value, out var employeeId) || employeeId <= 0)
            throw new UnauthorizedAccessException("EmployeeId claim is invalid.");

        return employeeId;
    }
}