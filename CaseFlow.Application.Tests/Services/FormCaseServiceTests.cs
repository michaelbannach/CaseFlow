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
    private readonly Mock<ILogger<FormCaseService>> _logger = new();

    private FormCaseService CreateSut()
        => new(_formCaseRepo.Object, _employeeRepo.Object, _logger.Object);

    // ---------- Helpers ----------
    private static Employee Employee(int id, UserRole role)
        => new() { Id = id, Role = role, ApplicationUserId = "u1", Name = "Test" };

    private static FormCase ValidFormCase()
        => new()
        {
            DepartmentId = 1,
            ApplicantName = "Max Mustermann",
            ApplicantStreet = "Musterstraße 1",
            ApplicantZip = 12345,
            ApplicantCity = "Musterstadt"
        };

    // ---------- GetById ----------
    [Fact]
    public async Task GetFormCaseByIdAsync_WhenIdInvalid_ThrowsArgumentException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetFormCaseByIdAsync(0));
    }

    // ---------- Create ----------
    [Fact]
    public async Task CreateFormCaseAsync_WhenActingEmployeeIdInvalid_ReturnsUnknownEmployee()
    {
        var sut = CreateSut();

        var (added, error) = await sut.CreateFormCaseAsync(0, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Unknown employee", error);
        _employeeRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _formCaseRepo.Verify(r => r.AddAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task CreateFormCaseAsync_WhenActorNotFound_ReturnsUnknownEmployee()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Employee?)null);

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Unknown employee", error);
        _formCaseRepo.Verify(r => r.AddAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task CreateFormCaseAsync_WhenActorIsStammdaten_ReturnsNotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Stammdaten));

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Not allowed", error);
        _formCaseRepo.Verify(r => r.AddAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task CreateFormCaseAsync_WhenActorNotErfasser_ReturnsNotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Sachbearbeiter));

        var sut = CreateSut();
        var (added, error) = await sut.CreateFormCaseAsync(1, ValidFormCase());

        Assert.False(added);
        Assert.Equal("Not allowed", error);
        _formCaseRepo.Verify(r => r.AddAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task CreateFormCaseAsync_WhenValid_SetsOwnerAndStatusAndSaves()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));
        _formCaseRepo.Setup(r => r.AddAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var sut = CreateSut();
        var formCase = ValidFormCase();

        var (added, error) = await sut.CreateFormCaseAsync(1, formCase);

        Assert.True(added);
        Assert.Null(error);

        Assert.Equal(1, formCase.CreateByEmployeeId);
        Assert.Equal(ProcessingStatus.Neu, formCase.Status);
        Assert.NotEqual(default, formCase.CreatedAt);
        Assert.NotEqual(default, formCase.UpdatedAt);

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
        _formCaseRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
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
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);
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
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.InKlaerung };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.Neu);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
        _formCaseRepo.Verify(r => r.UpdateAsync(It.IsAny<FormCase>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Erfasser_Allows_InKlaerung_To_Neu_OnlyForOwner()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));
        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.InKlaerung };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);
        _formCaseRepo.Setup(r => r.UpdateAsync(It.IsAny<FormCase>())).ReturnsAsync(true);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.Neu);

        Assert.True(updated);
        Assert.Null(error);
        Assert.Equal(ProcessingStatus.Neu, fc.Status);
        _formCaseRepo.Verify(r => r.UpdateAsync(fc), Times.Once);
    }

    [Fact]
    public async Task UpdateFormCaseStatusAsync_Erfasser_Blocks_InKlaerung_To_Neu_WhenNotOwner()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(Employee(99, UserRole.Erfasser));
        var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.InKlaerung };
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

        var sut = CreateSut();
        var (updated, error) = await sut.UpdateFormCaseStatusAsync(99, 10, ProcessingStatus.Neu);

        Assert.False(updated);
        Assert.Equal("Not allowed", error);
        _formCaseRepo.Verify(r => r.UpdateAsync(It.IsAny<FormCase>()), Times.Never);
    }

    // ---------- Delete ----------
    [Fact]
    public async Task DeleteFormCaseAsync_WhenActorNotSachbearbeiter_ReturnsNotAllowed()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));

        var sut = CreateSut();
        var (deleted, error) = await sut.DeleteFormCaseAsync(1, 10);

        Assert.False(deleted);
        Assert.Equal("Not allowed", error);
        _formCaseRepo.Verify(r => r.DeleteByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFormCaseAsync_WhenCaseNotFound_ReturnsNotFound()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((FormCase?)null);

        var sut = CreateSut();
        var (deleted, error) = await sut.DeleteFormCaseAsync(2, 10);

        Assert.False(deleted);
        Assert.Equal("FormCase not found", error);
        _formCaseRepo.Verify(r => r.DeleteByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFormCaseAsync_WhenValid_Deletes()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));
        _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new FormCase { Id = 10 });
        _formCaseRepo.Setup(r => r.DeleteByIdAsync(10)).ReturnsAsync(true);

        var sut = CreateSut();
        var (deleted, error) = await sut.DeleteFormCaseAsync(2, 10);

        Assert.True(deleted);
        Assert.Null(error);
        _formCaseRepo.Verify(r => r.DeleteByIdAsync(10), Times.Once);
    }
    
    [Fact]
public async Task UpdateFormCaseStatusAsync_Sachbearbeiter_Allows_InBearbeitung_To_Erledigt()
{
    _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));

    var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.InBearbeitung };
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
    _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Employee(2, UserRole.Sachbearbeiter));

    var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu };
    _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

    var sut = CreateSut();

    var (updated, error) = await sut.UpdateFormCaseStatusAsync(2, 10, ProcessingStatus.Erledigt);

    Assert.False(updated);
    Assert.Equal("Not allowed", error);
    Assert.Equal(ProcessingStatus.Neu, fc.Status);
    _formCaseRepo.Verify(r => r.UpdateAsync(It.IsAny<FormCase>()), Times.Never);
}

[Fact]
public async Task UpdateFormCaseStatusAsync_Erfasser_Blocks_Neu_To_InBearbeitung()
{
    _employeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Employee(1, UserRole.Erfasser));

    var fc = new FormCase { Id = 10, CreateByEmployeeId = 1, Status = ProcessingStatus.Neu };
    _formCaseRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(fc);

    var sut = CreateSut();

    var (updated, error) = await sut.UpdateFormCaseStatusAsync(1, 10, ProcessingStatus.InBearbeitung);

    Assert.False(updated);
    Assert.Equal("Not allowed", error);
    Assert.Equal(ProcessingStatus.Neu, fc.Status);
    _formCaseRepo.Verify(r => r.UpdateAsync(It.IsAny<FormCase>()), Times.Never);
}

}
