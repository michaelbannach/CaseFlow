using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Infrastructure.Repositories;

public class ClarificationMessageRepository : IClarificationMessageRepository
{
    private readonly AppDbContext _db;

    public ClarificationMessageRepository(AppDbContext db) => _db = db;

    public async Task<List<ClarificationMessage>> GetByFormCaseIdAsync(int formCaseId)
    {
        if (formCaseId <= 0) return new List<ClarificationMessage>();

        return await _db.ClarificationMessages
            .AsNoTracking()
            .Where(m => m.FormCaseId == formCaseId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(ClarificationMessage message)
    {
        if (message is null) return false;

        _db.ClarificationMessages.Add(message);
        return await _db.SaveChangesAsync() > 0;
    }
}