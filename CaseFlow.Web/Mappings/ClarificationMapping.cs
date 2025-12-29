using CaseFlow.Web.Dtos.ClarificationDtos;
using CaseFlow.Domain.Models;

namespace CaseFlow.Web.Mappings;

public static class ClarificationMapping
{
    public static ClarificationResponseDto ToDto(this ClarificationMessage entity)
        => new()
        {
            Id = entity.Id,
            FormCaseId = entity.FormCaseId,
            CreatedByEmployeeId = entity.CreatedByEmployeeId,
            Message = entity.Message,
            CreatedAt = entity.CreatedAt
        };
}