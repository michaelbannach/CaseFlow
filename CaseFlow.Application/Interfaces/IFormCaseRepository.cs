using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IFormCaseRepository
{
    Task<List<FormCase>> GetAllAsync();
    Task<FormCase?> GetByIdAsync(int id);

    Task<bool> AddAsync(FormCase formCase);
    Task<bool> UpdateAsync(FormCase formCase);
    Task<bool> DeleteByIdAsync(int id);
}