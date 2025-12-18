namespace CaseFlow.Web.Dtos.PdfAttachmentDtos;

public class AttachmentResponseDto
{
    public int Id { get; set; }
    public int FormCaseId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public int UploadedByEmployeeId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}