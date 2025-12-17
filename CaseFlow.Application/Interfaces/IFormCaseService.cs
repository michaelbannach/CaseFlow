using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IFormCaseService
{
    Task<List<FormCase>> GetAllFormCasesAsync();
    
    Task<FormCase?> GetFormCaseByIdAsync(int formCaseId);
      
    Task<(bool added, string? error)> CreateFormCaseAsync(FormCase formCase);
    
    Task<(bool updated, string? error )> UpdateFormCaseStatusAsync(int formCaseId, ProcessingStatus newStatus);
    
    Task<(bool deleted, string? error)> DeleteFormCaseAsync(FormCase formCase);
   
}