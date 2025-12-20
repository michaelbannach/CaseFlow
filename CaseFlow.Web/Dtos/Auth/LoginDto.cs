using System.ComponentModel.DataAnnotations;

namespace CaseFlow.Web.Dtos.Auth;

public record LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;
}