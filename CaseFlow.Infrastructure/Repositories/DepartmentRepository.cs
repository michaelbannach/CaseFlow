using CaseFlow.Domain.Models;
using CaseFlow.Application.Interfaces;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _db;

    public DepartmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Department>> GetAllAsync(CancellationToken ct = default)
    {
        return _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
    }
}