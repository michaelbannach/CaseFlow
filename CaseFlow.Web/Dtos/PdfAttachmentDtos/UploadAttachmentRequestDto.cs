namespace CaseFlow.Web.Dtos.PdfAttachmentDtos;

public class UploadAttachmentRequestDto
{
    public int UploadedByEmployeeId { get; set; }
    
    public IFormFile File { get; set; } = default!;
}