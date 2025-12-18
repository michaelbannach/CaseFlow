using CaseFlow.Domain.Models;
using CaseFlow.Web.Dtos.PdfAttachmentDtos;

namespace CaseFlow.Web.Mappings;

public static class PdfAttachmentMapping
{
    public static AttachmentResponseDto ToDto(this PdfAttachment entity)
    {
        return new AttachmentResponseDto
        {
            Id = entity.Id,
            FormCaseId = entity.FormCaseId,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            UploadedByEmployeeId = entity.UploadedByEmployeeId,
            UploadedAt = entity.UploadedAt
        };
    }
}