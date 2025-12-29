using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Services;

public sealed class ClarificationService : IClarificationService
{
    private readonly IClarificationMessageRepository _clarificationRepo;
    private readonly IFormCaseRepository _formCaseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ILogger<ClarificationService> _logger;

    public ClarificationService(
        IClarificationMessageRepository clarificationRepo,
        IFormCaseRepository formCaseRepo,
        IEmployeeRepository employeeRepo,
        ILogger<ClarificationService> logger)
    {
        _clarificationRepo = clarificationRepo;
        _formCaseRepo = formCaseRepo;
        _employeeRepo = employeeRepo;
        _logger = logger;
    }

    public async Task<List<ClarificationMessage>> GetByFormCaseAsync(int formCaseId)
    {
        if (formCaseId <= 0)
            throw new ArgumentException("Invalid FormCaseId");

        var formCaseExists = await _formCaseRepo.ExistsAsync(formCaseId);
        if (!formCaseExists)
            throw new KeyNotFoundException("FormCase not found");

        return await _clarificationRepo.GetByFormCaseIdAsync(formCaseId);
    }

    public async Task<(bool added, string? error, ClarificationMessage? created)> AddAsync(
        int actingEmployeeId,
        int formCaseId,
        string messageText)
    {
        if (actingEmployeeId <= 0) return (false, "Unknown employee", null);
        if (formCaseId <= 0) return (false, "Invalid FormCaseId", null);

        var actor = await _employeeRepo.GetByIdAsync(actingEmployeeId);
        if (actor is null) return (false, "Unknown employee", null);

        if (actor.Role == UserRole.Stammdaten)
            return (false, "Not allowed", null);

        var formCase = await _formCaseRepo.GetByIdAsync(formCaseId);
        if (formCase is null) return (false, "FormCase not found", null);

        // Option A: Nur erlauben, wenn der Case bereits in Klärung ist.
        if (formCase.Status != ProcessingStatus.InKlaerung)
            return (false, "FormCase is not in clarification", null);

        var msg = (messageText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(msg))
            return (false, "Message is required", null);

        if (msg.Length > 2000)
            return (false, "Message is too long (max 2000 chars)", null);

        var entity = new ClarificationMessage
        {
            FormCaseId = formCaseId,
            CreatedByEmployeeId = actor.Id,
            Message = msg,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var ok = await _clarificationRepo.AddAsync(entity);
        if (!ok)
        {
            _logger.LogError("AddAsync: Error while saving clarification message. CaseId={CaseId}, ActorId={ActorId}",
                formCaseId, actingEmployeeId);
            return (false, "Error while saving clarification message", null);
        }

        return (true, null, entity);
    }
}
