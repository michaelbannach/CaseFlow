using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Infrastructure.Repositories;

public class FormCaseRepository : IFormCaseRepository
{
    private readonly AppDbContext _db;

    public FormCaseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<FormCase>> GetAllAsync()
    {
       
        return await _db.FormCases
            .AsNoTracking()
            .OrderByDescending(fc => fc.CreatedAt)
            .ToListAsync();
    }

    public async Task<FormCase?> GetByIdAsync(int id)
    {
        return await _db.FormCases
            .FirstOrDefaultAsync(fc => fc.Id == id);
    }

    public async Task<bool> AddAsync(FormCase formCase)
    {
        _db.FormCases.Add(formCase);
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(FormCase formCase)
    {
        // Update ist ok, solange du im Service vorher das "existing" aus der DB holst (machst du)
        _db.FormCases.Update(formCase);
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteByIdAsync(int id)
    {
        var existing = await _db.FormCases.FirstOrDefaultAsync(fc => fc.Id == id);
        if (existing is null)
            return false;

        _db.FormCases.Remove(existing);
        return await _db.SaveChangesAsync() > 0;
    }
}