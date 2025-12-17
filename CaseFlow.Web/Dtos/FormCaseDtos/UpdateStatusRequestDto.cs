using CaseFlow.Domain.Enums;

namespace CaseFlow.Web.Dtos.FormCaseDtos;

public class UpdateStatusRequestDto
{
    public ProcessingStatus NewStatus { get; set; }
}