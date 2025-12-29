using System.ComponentModel.DataAnnotations;

namespace CaseFlow.Web.Dtos.ClarificationDtos;

public class CreateClarificationRequestDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}