using CaseFlow.Domain.Models;
using CaseFlow.Web.Dtos.FormCaseDtos;

namespace CaseFlow.Web.Mappings;

public static class FormCaseMapping
{
    public static FormCase ToEntity(this CreateFormCaseRequestDto dto)
    {
        return new FormCase
        {
            FormType = dto.FormType,
            DepartmentId = dto.DepartmentId,
            
            
            ApplicantName = dto.ApplicantName,
            ApplicantStreet = dto.ApplicantStreet,
            ApplicantZip = dto.ApplicantZip,
            ApplicantCity = dto.ApplicantCity,
            ApplicantPhone = dto.ApplicantPhone,
            ApplicantEmail = dto.ApplicantEmail,

            Subject = dto.Subject,
            Notes = dto.Notes,

            ServiceDescription = dto.ServiceDescription,
            Justification = dto.Justification,

            Amount = dto.Amount,
            CostType = dto.CostType,

            ChangeRequest = dto.ChangeRequest
        };
    }

    public static FormCaseResponseDto ToDto(this FormCase entity)
    {
        return new FormCaseResponseDto
        {
            Id = entity.Id,
            FormType = entity.FormType,
            Status = entity.Status,

            DepartmentId = entity.DepartmentId,
            CreatedByEmployeeId = entity.CreateByEmployeeId,

            ApplicantName = entity.ApplicantName,
            ApplicantStreet = entity.ApplicantStreet,
            ApplicantZip = entity.ApplicantZip,
            ApplicantCity = entity.ApplicantCity,
            ApplicantPhone = entity.ApplicantPhone,
            ApplicantEmail = entity.ApplicantEmail,

            Subject = entity.Subject,
            Notes = entity.Notes,

            ServiceDescription = entity.ServiceDescription,
            Justification = entity.Justification,

            Amount = entity.Amount,
            CostType = entity.CostType,

            ChangeRequest = entity.ChangeRequest,

            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}