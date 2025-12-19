namespace CaseFlow.Domain.Enums;

public sealed class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Name { get; set; } = default!;
    public UserRole Role { get; set; }
    public int? DepartmentId { get; set; }
}