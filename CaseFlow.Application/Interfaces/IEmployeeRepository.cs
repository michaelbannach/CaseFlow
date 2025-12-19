using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IEmployeeRepository
{
    
    Task<Employee?> GetByIdAsync(int id);
    
    Task<bool> AddAsync(Employee employee);

    Task<Employee?> GetByApplicationUserIdAsync(string applicationUserId);

    Task<bool> ExistsAsync(int id);
}