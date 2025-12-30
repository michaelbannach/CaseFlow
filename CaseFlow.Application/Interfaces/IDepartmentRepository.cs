using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync(CancellationToken ct = default);
}