using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;

        return await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> AddAsync(Employee employee)
    {
        if (employee == null)
            return false;

        _db.Employees.Add(employee);
        return await _db.SaveChangesAsync() > 0;
    }
    public async Task<Employee?> GetByApplicationUserIdAsync(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId)) return null;

        return await _db.Employees
            .FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        if (id <= 0) return false;

        return await _db.Employees
            .AnyAsync(e => e.Id == id);
    }
}