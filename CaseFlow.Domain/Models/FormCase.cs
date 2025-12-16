using CaseFlow.Domain.Enums;

namespace CaseFlow.Domain.Models;

public class FormCase
{
    public int Id { get; set; }

    public FormType FormType { get; set; } 
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Neu;
    
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
    
    public int CreateByEmployeeId { get; set; }
    public Employee CreateByEmployee { get; set; } = default!;
    
    
    public string ApplicantName { get; set; } = string.Empty;
    
    public string ApplicantStreet { get; set; } = string.Empty;
    
    public int ApplicantZip  { get; set; }
    
    public string ApplicantCity { get; set; } = string.Empty;
    
    public string ApplicantPhone { get; set; } = string.Empty;
    
    public string ApplicantEmail { get; set; } = string.Empty;
    
    
    public string? Subject { get; set; }
    public string? Notes { get;set; }
    
    //Leistungsantrag
    public string? ServiceDescription { get; set; }
    public string? Justification { get; set; }
    
    //Kostenantrag
    public decimal? Amount { get; set; }
    public string? CostType { get; set; }
    
    //Organisationsantrag
    public string? ChangeRequest { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PdfAttachment> Attachments { get; set; } = new List<PdfAttachment>();
    public ICollection<ClarificationMessage> ClarificationMessages { get; set; } = new List<ClarificationMessage>();
    
    
    
    
}