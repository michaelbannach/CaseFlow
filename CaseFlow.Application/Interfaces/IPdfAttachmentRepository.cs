using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IPdfAttachmentRepository
{
    Task<List<PdfAttachment>> GetByFormCaseIdAsync(int formCaseId);

    Task<PdfAttachment?> GetByIdAsync(int id);

    Task<bool> AddAsync(PdfAttachment attachment);

    Task<bool> DeleteAsync(PdfAttachment attachment);
    
    Task<bool> UpdateStorageKeyAsync(int attachmentId, string storageKey);

}