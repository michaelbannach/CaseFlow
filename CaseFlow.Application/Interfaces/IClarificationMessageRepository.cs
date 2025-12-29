using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IClarificationMessageRepository
{
    Task<List<ClarificationMessage>> GetByFormCaseIdAsync(int formCaseId);
    Task<bool> AddAsync(ClarificationMessage message);
}