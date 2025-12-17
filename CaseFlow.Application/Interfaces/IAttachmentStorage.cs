namespace CaseFlow.Application.Interfaces;

public interface IAttachmentStorage
{
    Task<(bool saved, string? error, string? storageKey)> SavePdfAsync(
        int formCaseId,
        int attachmentId,
        Stream fileStream);

    Task<(bool deleted, string? error)> DeleteAsync(string storageKey);
    
    Task<(Stream? stream, string? error)> OpenReadAsync(string storageKey);
}