using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IAttachmentService
{
    Task<List<PdfAttachment>> GetAttachmentsByFormCaseAsync(int formCaseId);

    Task<PdfAttachment?> GetAttachmentByIdAsync(int attachmentId);

    Task<(bool added, string? error)> AddAttachmentAsync(
        int formCaseId,
        PdfAttachment attachment,
        Stream fileStream);

    Task<(bool deleted, string? error)> DeleteAttachmentAsync(PdfAttachment attachment);
}