using CaseFlow.Application.Interfaces;
using CaseFlow.Application.Services;
using CaseFlow.Domain.Enums;
using CaseFlow.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CaseFlow.Application.Tests.Services;

public class FormCaseServiceTests
{
    private readonly Mock<IFormCaseRepository> _formCaseRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IPdfAttachmentRepository> _attachmentRepo = new();
    private readonly Mock<ILogger<FormCaseService>> _logger = new();

    private FormCaseService CreateSut()
        => new(
            _formCaseRepo.Object,
            _employeeRepo.Object,
            _attachmentRepo.Object,
            _logger.Object);

    private static Employee Employee(int id, UserRole role)
        => new()
        {
            Id = id,
            Role = role,
            ApplicationUserId = "u1",
            Name = "Test"
        };

    private static FormCase ValidFormCase()
        => new()
        {
            DepartmentId = 1,
            ApplicantName = "Max Mustermann",
            ApplicantStreet = "Musterstraße 1",
            ApplicantZip = 12345,
            ApplicantCity = "Berlin",
            ApplicantEmail = "max@example.com",
            ApplicantPhone = "123",
            Subject = "Betreff",
            Notes = "Notizen"
        };

    // ---------- GetAll ----------
    [Fact]
    public async Task GetAllFormCasesAsync_ReturnsList()
    {
        _formCaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FormCase> { new() { Id = 1 } });

        var sut = CreateSut();
        var result = await sut.GetAllFormCasesAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    // ---------- GetById ----------
    [Fact]
    public async Task GetFormCaseByIdAsync_InvalidId_Throws()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetFormCaseByIdAsync(0));
    }

    [Fact]
    public async Task GetFormCaseByIdAsync_ValidId_ReturnsCase()
    {
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new FormCase { Id = 10 });

        var sut = CreateSut();
        var result = await sut.GetFormCaseByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
    }

    // ---------- Create ----------
    [Fact]
    public async Task CreateFormCaseAsync_UnknownEmployee_ReturnsFalse()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Employee?)null);

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Unknown employee", error);
    }

    [Fact]
    public async Task CreateFormCaseAsync_Stammdaten_NotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Stammdaten));

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task CreateFormCaseAsync_NotErfasser_NotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Sachbearbeiter));

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task CreateFormCaseAsync_Valid_Erfasser_AddsCase()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));
        _formCaseRepo.Setup(r => r.AddAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var formCase = ValidFormCase();
        var sut = CreateSut();

        var (added, error) = await sut.CreateFormCaseAsync(1, formCase);

        Assert.True(added);
        Assert.Null(error);
        Assert.Equal(1, formCase.CreateByEmployeeId);
        Assert.Equal(ProcessingStatus.Neu, formCase.Status);

        _formCaseRepo.Verify(r => r.AddAsync(It.Is<FormCase>(fc => fc == formCase)), Times.Once);
    }

    // ---------- UpdateStatus ----------
    [Fact]
    public async Task UpdateFormCaseStatusAsync_WhenActorIsStammdaten_ReturnsNotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Stammdaten));

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.InBearbeitung);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_WhenFormCaseNotFound_ReturnsNotFound()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((FormCase?)null);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.InBearbeitung);

        Assert.False(updated);
        Assert.Equal("FormCase not found", error);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_WhenSameStatus_ReturnsTrueAndDoesNotUpdate()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new FormCase
        {
            Id = 10,
            DepartmentId = 1,
            CreateByEmployeeId = 1,
            Status = ProcessingStatus.Neu
        });

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.Neu);

        Assert.True(updated);
        Assert.Null(error);
        _formCaseRepo.Verify(r => r.UpdateAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Sachbearbeiter_Allows_Neu_To_InBearbeitung()
    {
        var actor = Employee(2, UserRole.Sachbearbeiter);
        actor.DepartmentId = 1;
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(actor);

        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu, DepartmentId = 1 };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        // Required for leaving Neu (falls deine Service-Regel das so prüft)
        _attachmentRepo.Setup(r => r.GetByFormCaseIdAsync(10))
            .ReturnsAsync(new List<PdfAttachment> { new PdfAttachment { Id = 1, FormCaseId = 10 } });

        _formCaseRepo.Setup(r => r.UpdateAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.InBearbeitung);

        Assert.True(updated);
        Assert.Null(error);
        Assert.Equal(ProcessingStatus.InBearbeitung, fc.Status);

        _formCaseRepo.Verify(r => r.UpdateAsync(fc), Times.Once);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Sachbearbeiter_Blocks_WhenCurrentIsInKlaerung()
    {
        var actor = Employee(2, UserRole.Sachbearbeiter);
        actor.DepartmentId = 1;
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(actor);

        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, DepartmentId = 1, Status = ProcessingStatus.InKlaerung };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.InBearbeitung);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Erfasser_Allows_InKlaerung_To_Neu_OnlyForOwner()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));

        var fc = new FormCase
        {
            Id = 10,
            DepartmentId = 1,
            CreateByEmployeeId = 1,
            Status = ProcessingStatus.InKlaerung,
            ProcessingEmployeeId = 2
        };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);
        _formCaseRepo.Setup(r => r.UpdateAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.Neu);

        Assert.True(updated);
        Assert.Null(error);
        Assert.Equal(ProcessingStatus.Neu, fc.Status);
        Assert.Null(fc.ProcessingEmployeeId); // lock released
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Erfasser_Blocks_InKlaerung_To_Neu_WhenNotOwner()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));

        var fc = new FormCase
        {
            Id = 10,
            DepartmentId = 1,
            CreateByEmployeeId = 999,
            Status = ProcessingStatus.InKlaerung
        };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.Neu);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Sachbearbeiter_Allows_InBearbeitung_To_Erledigt()
    {
        var actor = Employee(2, UserRole.Sachbearbeiter);
        actor.DepartmentId = 1;
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(actor);

        var fc = new FormCase
        {
            Id = 10,
            CreateByEmployeeId = 1,
            Status = ProcessingStatus.InBearbeitung,
            DepartmentId = 1,
            ProcessingEmployeeId = 2 // WICHTIG: Lock owner
        };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        _formCaseRepo.Setup(r => r.UpdateAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.Erledigt);

        Assert.True(updated);
        Assert.Null(error);
        Assert.Equal(ProcessingStatus.Erledigt, fc.Status);

        _formCaseRepo.Verify(r => r.UpdateAsync(fc), Times.Once);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Sachbearbeiter_Blocks_Neu_To_Erledigt()
    {
        var actor = Employee(2, UserRole.Sachbearbeiter);
        actor.DepartmentId = 1;
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(actor);

        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu, DepartmentId = 1 };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.Erledigt);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Erfasser_Blocks_Neu_To_InBearbeitung()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));

        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu, DepartmentId = 1 };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.InBearbeitung);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
    }
}
