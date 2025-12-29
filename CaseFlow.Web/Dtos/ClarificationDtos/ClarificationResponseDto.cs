namespace CaseFlow.Web.Dtos.ClarificationDtos;

public class ClarificationResponseDto
{
    public int Id { get; set; }
    public int FormCaseId { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}