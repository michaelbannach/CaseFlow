namespace CaseFlow.Domain.Models;

public class PdfAttachment
{
    public int Id { get; set; }

    public int FormCaseId { get; set; }
    public FormCase FormCase { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }

    // z.B. "cases/{caseId}/{attachmentId}.pdf"
    public string StorageKey { get; set; } = default!;

    public int UploadedByEmployeeId { get; set; }
    public Employee UploadedByEmployee { get; set; } = default!;

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}