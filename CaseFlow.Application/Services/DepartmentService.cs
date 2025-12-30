using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;

    public DepartmentService(IDepartmentRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }
}