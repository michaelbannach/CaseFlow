using System.ComponentModel.DataAnnotations;
using CaseFlow.Domain.Enums;

namespace CaseFlow.Web.Dtos.Auth;

public record RegisterDto
{
    [Required]
    [MaxLength(50)]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = null!;

    [Required]
    public UserRole Role { get; init; }

    public int? DepartmentId { get; init; }
}