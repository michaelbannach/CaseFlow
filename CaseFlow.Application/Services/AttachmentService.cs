using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CaseFlow.Application.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IPdfAttachmentRepository _attachmentRepository;
    private readonly IFormCaseRepository _formCaseRepository;
    private readonly IAttachmentStorage _storage;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IPdfAttachmentRepository attachmentRepository,
        IFormCaseRepository formCaseRepository,
        IAttachmentStorage storage,
        ILogger<AttachmentService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _formCaseRepository = formCaseRepository;
        _storage = storage;
        _logger = logger;
    }

    public async Task<List<PdfAttachment>> GetAttachmentsByFormCaseAsync(int formCaseId)
    {
        if (formCaseId <= 0)
        {
            _logger.LogWarning("GetAttachmentsByFormCaseAsync: Invalid formCaseId {Id}", formCaseId);
            return new List<PdfAttachment>();
        }

        return await _attachmentRepository.GetByFormCaseIdAsync(formCaseId);
    }

    public async Task<PdfAttachment?> GetAttachmentByIdAsync(int attachmentId)
    {
        if (attachmentId <= 0)
        {
            _logger.LogWarning("GetAttachmentByIdAsync: attachmentId must be greater than zero");
            throw new ArgumentException("attachmentId must be greater than zero", nameof(attachmentId));
        }

        return await _attachmentRepository.GetByIdAsync(attachmentId);
    }

    public async Task<(bool added, string? error)> AddAttachmentAsync(
        int formCaseId,
        PdfAttachment attachment,
        Stream fileStream)
    {
        if (formCaseId <= 0)
        {
            _logger.LogWarning("AddAttachmentAsync: Invalid formCaseId {Id}", formCaseId);
            return (false, "Invalid FormCaseId");
        }

        if (attachment is null)
        {
            _logger.LogWarning("AddAttachmentAsync: attachment is null");
            return (false, "Attachment is null");
        }

        if (fileStream is null)
        {
            _logger.LogWarning("AddAttachmentAsync: fileStream is null");
            return (false, "File is missing");
        }

        
        var formCase = await _formCaseRepository.GetByIdAsync(formCaseId);
        if (formCase is null)
        {
            _logger.LogWarning("AddAttachmentAsync: FormCaseId {Id} not found", formCaseId);
            return (false, "FormCase not found");
        }

        
        if (string.IsNullOrWhiteSpace(attachment.FileName))
        {
            _logger.LogWarning("AddAttachmentAsync: FileName is required");
            return (false, "FileName is required");
        }

        if (!string.Equals(attachment.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("AddAttachmentAsync: Only PDF allowed. ContentType={ContentType}", attachment.ContentType);
            return (false, "Only PDF files are allowed");
        }

        if (attachment.SizeBytes <= 0)
        {
            _logger.LogWarning("AddAttachmentAsync: SizeBytes is invalid {Size}", attachment.SizeBytes);
            return (false, "File size is invalid");
        }

        if (attachment.UploadedByEmployeeId <= 0)
        {
            _logger.LogWarning("AddAttachmentAsync: UploadedByEmployeeId is invalid");
            return (false, "UploadedByEmployeeId is required");
        }

        
        attachment.FormCaseId = formCaseId;
        attachment.UploadedAt = DateTimeOffset.UtcNow;

        
        attachment.StorageKey = "pending";

        var ok = await _attachmentRepository.AddAsync(attachment);
        if (!ok)
        {
            _logger.LogError("AddAttachmentAsync: Error while saving attachment metadata");
            return (false, "Error while saving attachment metadata");
        }

       
        var (saved, saveError, storageKey) = await _storage.SavePdfAsync(formCaseId, attachment.Id, fileStream);
        if (!saved || string.IsNullOrWhiteSpace(storageKey))
        {
            _logger.LogError("AddAttachmentAsync: Error while saving file content: {Error}", saveError);

           
            await _attachmentRepository.DeleteAsync(attachment);

            return (false, saveError ?? "Error while saving file");
        }

        
        var updated = await _attachmentRepository.UpdateStorageKeyAsync(attachment.Id, storageKey);
        if (!updated)
        {
            _logger.LogError("AddAttachmentAsync: Failed to update StorageKey for attachment {Id}", attachment.Id);

            
            await _storage.DeleteAsync(storageKey);
            await _attachmentRepository.DeleteAsync(attachment);

            return (false, "Error while finalizing attachment");
        }

        return (true, null);
    }

    public async Task<(bool deleted, string? error)> DeleteAttachmentAsync(PdfAttachment attachment)
    {
        if (attachment is null)
        {
            _logger.LogWarning("DeleteAttachmentAsync: attachment is null");
            return (false, "Attachment must not be null");
        }

        if (attachment.Id <= 0)
        {
            _logger.LogWarning("DeleteAttachmentAsync: Invalid attachmentId {Id}", attachment.Id);
            return (false, "Invalid attachmentId");
        }

        
        if (!string.IsNullOrWhiteSpace(attachment.StorageKey))
        {
            var (fileDeleted, fileError) = await _storage.DeleteAsync(attachment.StorageKey);
            if (!fileDeleted)
            {
                _logger.LogWarning("DeleteAttachmentAsync: Could not delete file for attachment {Id}: {Error}",
                    attachment.Id, fileError);
            }
        }

        var deleted = await _attachmentRepository.DeleteAsync(attachment);
        if (!deleted)
        {
            _logger.LogError("DeleteAttachmentAsync: Error deleting attachment metadata {Id}", attachment.Id);
            return (false, "Error deleting attachment");
        }

        return (true, null);
    }
    public async Task<(Stream? stream, string? fileName, string? contentType, string? error)>
        DownloadAsync(int attachmentId)
    {
        if (attachmentId <= 0)
        {
            _logger.LogWarning("DownloadAsync: Invalid attachmentId {Id}", attachmentId);
            return (null, null, null, "Invalid attachmentId");
        }

        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (attachment is null)
        {
            _logger.LogWarning("DownloadAsync: AttachmentId {Id} not found", attachmentId);
            return (null, null, null, "Attachment not found");
        }

        if (string.IsNullOrWhiteSpace(attachment.StorageKey))
        {
            _logger.LogWarning("DownloadAsync: AttachmentId {Id} has no StorageKey", attachmentId);
            return (null, null, null, "Attachment has no file reference");
        }

        var (stream, error) = await _storage.OpenReadAsync(attachment.StorageKey);
        if (stream is null)
        {
            _logger.LogError("DownloadAsync: File not found for attachmentId {Id}", attachmentId);
            return (null, null, null, error ?? "File not found");
        }

        return (stream, attachment.FileName, attachment.ContentType, null);
    }

}
