namespace CaseFlow.Web.Dtos.PdfAttachmentDtos;

public class UploadAttachmentRequestDto
{
   public IFormFile File { get; set; } = default!;
}