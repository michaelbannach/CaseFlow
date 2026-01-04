using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;

namespace CaseFlow.Application.Services;

public class ClarificationService : IClarificationService
{
    private readonly IClarificationMessageRepository _clarificationRepository;
    private readonly IFormCaseRepository _formCaseRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<ClarificationService> _logger;

    public ClarificationService(
        IClarificationMessageRepository clarificationRepository,
        IFormCaseRepository formCaseRepository,
        IEmployeeRepository employeeRepository,
        ILogger<ClarificationService> logger)
    {
        _clarificationRepository = clarificationRepository;
        _formCaseRepository = formCaseRepository;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<List<ClarificationMessage>> GetByFormCaseAsync(
        int actingEmployeeId,
        int formCaseId)
    {
        if (actingEmployeeId <= 0)
            throw new ArgumentException("Unknown employee");

        if (formCaseId <= 0)
            throw new ArgumentException("Invalid FormCaseId");

        var actor = await _employeeRepository.GetByIdAsync(actingEmployeeId)
                    ?? throw new ArgumentException("Unknown employee");

        var formCase = await _formCaseRepository.GetByIdAsync(formCaseId)
                       ?? throw new KeyNotFoundException("FormCase not found");

        // Sichtbarkeitsregeln
        if (actor.Role == UserRole.Stammdaten)
            return await _clarificationRepository.GetByFormCaseIdAsync(formCaseId);

        if (actor.Role == UserRole.Erfasser)
        {
            if (formCase.CreateByEmployeeId != actor.Id)
                throw new UnauthorizedAccessException("Not allowed");

            return await _clarificationRepository.GetByFormCaseIdAsync(formCaseId);
        }

        if (actor.Role == UserRole.Sachbearbeiter)
        {
            if (formCase.DepartmentId != actor.DepartmentId)
                throw new UnauthorizedAccessException("Not allowed");

            return await _clarificationRepository.GetByFormCaseIdAsync(formCaseId);
        }

        throw new UnauthorizedAccessException("Not allowed");
    }

    public async Task<(bool added, string? error, ClarificationMessage? created)>
        AddAsync(int actingEmployeeId, int formCaseId, string message)

    {
        if (actingEmployeeId <= 0)
            return (false, "Unknown employee", null);

        if (formCaseId <= 0)
            return (false, "Invalid FormCaseId", null);

        if (string.IsNullOrWhiteSpace(message))
            return (false, "Message is required", null);

        var actor = await _employeeRepository.GetByIdAsync(actingEmployeeId);
        if (actor is null)
            return (false, "Unknown employee", null);

       
        if (actor.Role != UserRole.Sachbearbeiter)
            return (false, "Not allowed", null);

        var formCase = await _formCaseRepository.GetByIdAsync(formCaseId);
        if (formCase is null)
            return (false, "FormCase not found", null);

        // Abteilung prüfen
        if (formCase.DepartmentId != actor.DepartmentId)
            return (false, "Not allowed", null);

        // Nur im Status InBearbeitung
        if (formCase.Status != ProcessingStatus.InBearbeitung)
            return (false, "Not allowed", null);

        var clarification = new ClarificationMessage
        {
            FormCaseId = formCase.Id,
            CreatedByEmployeeId = actor.Id,
            Message = message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var ok = await _clarificationRepository.AddAsync(clarification);
        if (!ok)
        {
            _logger.LogError(
                "AddClarification failed for FormCaseId {FormCaseId}", formCaseId);
            return (false, "Error while saving clarification message", null);
        }

        return (true, null, clarification);
    }
}
