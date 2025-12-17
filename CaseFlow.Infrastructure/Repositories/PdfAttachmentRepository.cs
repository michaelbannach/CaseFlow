using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Infrastructure.Repositories;

public class PdfAttachmentRepository : IPdfAttachmentRepository
{
    private readonly AppDbContext _db;

    public PdfAttachmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PdfAttachment>> GetByFormCaseIdAsync(int formCaseId)
    {
        return await _db.PdfAttachments
            .AsNoTracking()
            .Where(a => a.FormCaseId == formCaseId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<PdfAttachment?> GetByIdAsync(int id)
    {
        return await _db.PdfAttachments
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> AddAsync(PdfAttachment attachment)
    {
        _db.PdfAttachments.Add(attachment);
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(PdfAttachment attachment)
    {
        _db.PdfAttachments.Remove(attachment);
        return await _db.SaveChangesAsync() > 0;
    }
    
    public async Task<bool> UpdateStorageKeyAsync(int attachmentId, string storageKey)
    {
        var existing = await _db.PdfAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (existing is null) return false;

        existing.StorageKey = storageKey;
        return await _db.SaveChangesAsync() > 0;
    }

}