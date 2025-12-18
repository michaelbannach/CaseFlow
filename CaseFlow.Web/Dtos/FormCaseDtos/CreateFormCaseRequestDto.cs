using CaseFlow.Domain.Enums;

namespace CaseFlow.Web.Dtos.FormCaseDtos;

public class CreateFormCaseRequestDto
{
    public FormType FormType { get; set; }

    public int ActingEmployeeId { get; set; }

    public int DepartmentId { get; set; }
    public int CreatedByEmployeeId { get; set; }

    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantStreet { get; set; } = string.Empty;
    public int ApplicantZip { get; set; }
    public string ApplicantCity { get; set; } = string.Empty;

    public string ApplicantPhone { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;

    public string? Subject { get; set; }
    public string? Notes { get; set; }

    public string? ServiceDescription { get; set; }
    public string? Justification { get; set; }

    public decimal? Amount { get; set; }
    public string? CostType { get; set; }

    public string? ChangeRequest { get; set; }
}
