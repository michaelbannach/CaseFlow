namespace CaseFlow.Domain.Models;

public class ClarificationMessage
{
    public int Id { get; set; }
    
    public int FormCaseId { get; set; }
    public FormCase FormCase { get; set; } = default!;
    
    public int CreatedByEmployeeId { get; set; }
    public Employee CreatedByEmployee { get; set; } = default!;
    
    public string Message { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}