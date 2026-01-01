using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CaseFlow.Application.Services;

public class FormCaseService : IFormCaseService
{
    private readonly IFormCaseRepository _formCaseRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<FormCaseService> _logger;
    private readonly IPdfAttachmentRepository _attachmentRepository;


    public FormCaseService(
        IFormCaseRepository formCaseRepository,
        IEmployeeRepository employeeRepository,
        IPdfAttachmentRepository attachmentRepository,
        ILogger<FormCaseService> logger)
        
    {
        _formCaseRepository = formCaseRepository;
        _employeeRepository = employeeRepository;
        _attachmentRepository = attachmentRepository;
        _logger = logger;
    }

    public async Task<List<FormCase>> GetAllFormCasesAsync()
    {
        return await _formCaseRepository.GetAllAsync();
    }

    public async Task<FormCase?> GetFormCaseByIdAsync(int formCaseId)
    {
        if (formCaseId <= 0)
        {
            _logger.LogWarning("GetFormCaseByIdAsync: formCaseId must be greater than zero");
            throw new ArgumentException("formCaseId must be greater than zero", nameof(formCaseId));
        }

        return await _formCaseRepository.GetByIdAsync(formCaseId);
    }

    public async Task<(bool added, string? error)> CreateFormCaseAsync(int actingEmployeeId, FormCase formCase)
    {
        if (actingEmployeeId <= 0)
            return (false, "Unknown employee");

        var actor = await _employeeRepository.GetByIdAsync(actingEmployeeId);
        if (actor is null)
            return (false, "Unknown employee");

       
        if (actor.Role == UserRole.Stammdaten)
            return (false, "Not allowed");

        
        if (actor.Role != UserRole.Erfasser)
            return (false, "Not allowed");

        if (formCase is null)
            return (false, "FormCase is null");

      
        if (formCase.DepartmentId <= 0)
            return (false, "Department is required");

        if (string.IsNullOrWhiteSpace(formCase.ApplicantName))
            return (false, "ApplicantName is required");

        if (string.IsNullOrWhiteSpace(formCase.ApplicantStreet))
            return (false, "ApplicantStreet is required");

        if (formCase.ApplicantZip <= 0)
            return (false, "ApplicantZip is invalid");

        if (string.IsNullOrWhiteSpace(formCase.ApplicantCity))
            return (false, "ApplicantCity is required");

     
        formCase.CreateByEmployeeId = actor.Id;
        formCase.Status = ProcessingStatus.Neu;

        if (formCase.CreatedAt == default)
            formCase.CreatedAt = DateTimeOffset.UtcNow;

        formCase.UpdatedAt = DateTimeOffset.UtcNow;

        var ok = await _formCaseRepository.AddAsync(formCase);
        if (!ok)
        {
            _logger.LogError("CreateFormCaseAsync: Error while saving FormCase");
            return (false, "Error while saving");
        }

        return (true, null);
    }

    public async Task<(bool updated, string? error)> UpdateFormCaseStatusAsync(
    int actingEmployeeId,
    int formCaseId,
    ProcessingStatus newStatus)
{
    if (actingEmployeeId <= 0)
        return (false, "Unknown employee");

    if (formCaseId <= 0)
        return (false, "Invalid FormCaseId");

    var actor = await _employeeRepository.GetByIdAsync(actingEmployeeId);
    if (actor is null)
        return (false, "Unknown employee");

    if (actor.Role == UserRole.Stammdaten)
        return (false, "Not allowed");

    var formCase = await _formCaseRepository.GetByIdAsync(formCaseId);
    if (formCase is null)
        return (false, "FormCase not found");

    var currentStatus = formCase.Status;

    if (currentStatus == newStatus)
        return (true, null);

    var isOwner = formCase.CreateByEmployeeId == actor.Id;

    // 1) Base transition rules
    if (!IsTransitionAllowed(currentStatus, newStatus, actor.Role, isOwner))
    {
        _logger.LogWarning(
            "UpdateFormCaseStatusAsync: Transition not allowed. Actor={ActorId}, Role={Role}, Case={CaseId}, From={From}, To={To}, Owner={Owner}",
            actor.Id, actor.Role, formCase.Id, currentStatus, newStatus, isOwner);

        return (false, "Not allowed");
    }

    // 2) PDF rule: leaving Neu requires at least one attachment
    if (currentStatus == ProcessingStatus.Neu && newStatus != ProcessingStatus.Neu)
    {
        var attachments = await _attachmentRepository.GetByFormCaseIdAsync(formCaseId);
        if (attachments.Count == 0)
            return (false, "At least one PDF attachment is required");
    }

    // 3) Department rule: Sachbearbeiter only in own department
    if (actor.Role == UserRole.Sachbearbeiter && actor.DepartmentId != formCase.DepartmentId)
        return (false, "Not allowed");

    // 4) Lock rules (make sure property name matches your FormCase model!)
    if (actor.Role == UserRole.Sachbearbeiter)
    {
        // Neu -> InBearbeitung: lock setzen / prüfen
        if (currentStatus == ProcessingStatus.Neu && newStatus == ProcessingStatus.InBearbeitung)
        {
            if (formCase.ProcessingEmployeeId is null)
                formCase.ProcessingEmployeeId = actor.Id;
            else if (formCase.ProcessingEmployeeId != actor.Id)
                return (false, "Not allowed");
        }

        // InBearbeitung -> InKlaerung/Erledigt: nur lock-owner
        if (currentStatus == ProcessingStatus.InBearbeitung &&
            (newStatus == ProcessingStatus.InKlaerung || newStatus == ProcessingStatus.Erledigt))
        {
            if (formCase.ProcessingEmployeeId != actor.Id)
                return (false, "Not allowed");
        }
    }

    if (actor.Role == UserRole.Erfasser)
    {
        // InKlaerung -> Neu: lock freigeben
        if (currentStatus == ProcessingStatus.InKlaerung && newStatus == ProcessingStatus.Neu)
            formCase.ProcessingEmployeeId = null;
    }

    // 5) Apply & save
    formCase.Status = newStatus;
    formCase.UpdatedAt = DateTimeOffset.UtcNow;

    var ok = await _formCaseRepository.UpdateAsync(formCase);
    if (!ok)
    {
        _logger.LogError("UpdateFormCaseStatusAsync: Failed to update FormCaseId {Id}", formCase.Id);
        return (false, "Error while updating status");
    }

    return (true, null);
}


    public async Task<(bool deleted, string? error)> DeleteFormCaseAsync(int actingEmployeeId, int formCaseId)
    {
        if (actingEmployeeId <= 0)
            return (false, "Unknown employee");

        if (formCaseId <= 0)
            return (false, "Invalid FormCaseId");

        var actor = await _employeeRepository.GetByIdAsync(actingEmployeeId);
        if (actor is null)
            return (false, "Unknown employee");

      
        if (actor.Role == UserRole.Stammdaten)
            return (false, "Not allowed");
        
        if (actor.Role != UserRole.Sachbearbeiter)
            return (false, "Not allowed");

        var existing = await _formCaseRepository.GetByIdAsync(formCaseId);
        if (existing is null)
            return (false, "FormCase not found");
        
        var deleted = await _formCaseRepository.DeleteByIdAsync(formCaseId);
        if (!deleted)
        {
            _logger.LogError("DeleteFormCaseAsync: Error deleting FormCaseId {Id}", formCaseId);
            return (false, "Error deleting FormCase");
        }

        return (true, null);
    }

    private static bool IsTransitionAllowed(
        ProcessingStatus current,
        ProcessingStatus next,
        UserRole role,
        bool isOwner)
    {
        if (role == UserRole.Stammdaten)
            return false;

       
        if (role == UserRole.Erfasser)
        {
            if (!isOwner) return false;

            return current == ProcessingStatus.InKlaerung
                   && next == ProcessingStatus.Neu;
        }
        
        if (role == UserRole.Sachbearbeiter)
        {
            if (current == ProcessingStatus.InKlaerung) return false;

            return (current == ProcessingStatus.Neu && next == ProcessingStatus.InBearbeitung)
                   || (current == ProcessingStatus.InBearbeitung && next == ProcessingStatus.InKlaerung)
                   || (current == ProcessingStatus.InBearbeitung && next == ProcessingStatus.Erledigt);
        }

        return false;
    }
}
