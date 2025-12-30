using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<Department>> GetAllAsync();
}