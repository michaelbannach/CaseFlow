using CaseFlow.Domain.Enums;

namespace CaseFlow.Web.Dtos.FormCaseDtos;

public class UpdateStatusRequestDto
{
    public int ActingEmployeeId { get; set; }
    public ProcessingStatus NewStatus { get; set; }
}