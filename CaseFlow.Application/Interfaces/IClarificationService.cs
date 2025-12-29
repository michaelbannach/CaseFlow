using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Interfaces;

public interface IClarificationService
{
    Task<List<ClarificationMessage>> GetByFormCaseAsync(int formCaseId);

    Task<(bool added, string? error, ClarificationMessage? created)> AddAsync(
        int actingEmployeeId,
        int formCaseId,
        string messageText);
}