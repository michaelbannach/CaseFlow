

using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;


namespace CaseFlow.Application.Services;

public class FormCaseService : IFormCaseService
{
    private readonly IFormCaseRepository _formCaseRepository;
    private readonly ILogger<FormCaseService> _logger;

    public FormCaseService(IFormCaseRepository formCaseRepository, ILogger<FormCaseService> logger)
    {
        _formCaseRepository = formCaseRepository;
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
            _logger.LogWarning("GetFormCaseByIdAsync: FormCaseId must be greater than zero");
            throw new ArgumentException("FormCaseId must be greater than zero", nameof(formCaseId));
        }

        return await _formCaseRepository.GetByIdAsync(formCaseId);
    }

    public async Task<(bool added, string? error)> CreateFormCaseAsync(FormCase formCase)
    {
        if (formCase is null)
        {
            _logger.LogWarning("AddFormCaseAsync: FormCase is null");
            return (false, "FormCase is null");
        }

        // Required FKs (based on your entity)
        if (formCase.DepartmentId <= 0)
        {
            _logger.LogWarning("AddFormCaseAsync: DepartmentId is missing");
            return (false, "Department is required");
        }

        if (formCase.CreateByEmployeeId <= 0)
        {
            _logger.LogWarning("AddFormCaseAsync: CreateByEmployeeId is missing");
            return (false, "Creator (employee) is required");
        }

        // Basic applicant validation (you can tighten/loosen later)
        if (string.IsNullOrWhiteSpace(formCase.ApplicantName))
        {
            _logger.LogWarning("AddFormCaseAsync: ApplicantName is required");
            return (false, "ApplicantName is required");
        }

        if (string.IsNullOrWhiteSpace(formCase.ApplicantStreet))
        {
            _logger.LogWarning("AddFormCaseAsync: ApplicantStreet is required");
            return (false, "ApplicantStreet is required");
        }

        if (formCase.ApplicantZip <= 0)
        {
            _logger.LogWarning("AddFormCaseAsync: ApplicantZip is invalid");
            return (false, "ApplicantZip is invalid");
        }

        if (string.IsNullOrWhiteSpace(formCase.ApplicantCity))
        {
            _logger.LogWarning("AddFormCaseAsync: ApplicantCity is required");
            return (false, "ApplicantCity is required");
        }

        // Defaults
        if (formCase.CreatedAt == default)
            formCase.CreatedAt = DateTimeOffset.UtcNow;

        formCase.UpdatedAt = DateTimeOffset.UtcNow;

        // Status already defaults to Neu in your entity – ensure it's not an undefined value
        // If you ever add "0" enum value, you can handle it here.

        var ok = await _formCaseRepository.AddAsync(formCase);
        if (!ok)
        {
            _logger.LogError("AddFormCaseAsync: Error while saving FormCase");
            return (false, "Error while saving");
        }

        return (true, null);
    }

    public async Task<(bool updated, string? error)> UpdateFormCaseStatusAsync(int formCaseId, ProcessingStatus newStatus)
    {
        if (formCaseId <= 0)
        {
            _logger.LogWarning("UpdateFormCaseStatusAsync: Invalid FormCaseId: {Id}", formCaseId);
            return (false, "Invalid FormCaseId");
        }

        var existing = await _formCaseRepository.GetByIdAsync(formCaseId);
        if (existing is null)
        {
            _logger.LogWarning("UpdateFormCaseStatusAsync: FormCaseId {Id} not found", formCaseId);
            return (false, $"FormCaseId {formCaseId} not found");
        }

        if (existing.Status == newStatus)
        {
            _logger.LogInformation("UpdateFormCaseStatusAsync: Status unchanged for FormCaseId {Id}", formCaseId);
            return (true, null);
        }

        existing.Status = newStatus;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        var ok = await _formCaseRepository.UpdateAsync(existing);
        if (!ok)
        {
            _logger.LogError("UpdateFormCaseStatusAsync: Failed to update FormCaseId {Id}", formCaseId);
            return (false, "Error while updating status");
        }

        return (true, null);
    }

    public async Task<(bool deleted, string? error)> DeleteFormCaseAsync(FormCase formCase)
    {
        if (formCase is null)
        {
            _logger.LogWarning("DeleteFormCaseAsync: FormCase is null");
            return (false, "FormCase must not be null");
        }

        if (formCase.Id <= 0)
        {
            _logger.LogWarning("DeleteFormCaseAsync: Invalid FormCaseId");
            return (false, "Invalid FormCaseId");
        }

        _logger.LogInformation("Delete FormCase {FormCaseId}", formCase.Id);

        var deleted = await _formCaseRepository.DeleteByIdAsync(formCase.Id);
        if (!deleted)
        {
            _logger.LogError("DeleteFormCaseAsync: Error deleting FormCaseId {FormCaseId}", formCase.Id);
            return (false, "Error deleting FormCase");
        }

        return (true, null);
    }
}
