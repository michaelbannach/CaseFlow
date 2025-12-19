using System.Text;
using CaseFlow.Application.Interfaces;
using CaseFlow.Application.Services;
using CaseFlow.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CaseFlow.Application.Tests.Services;

public class AttachmentServiceTests
{
    private readonly Mock<IPdfAttachmentRepository> _attachmentRepo = new();
    private readonly Mock<IFormCaseRepository> _formCaseRepo = new();
    private readonly Mock<IAttachmentStorage> _storage = new();
    private readonly Mock<ILogger<AttachmentService>> _logger = new();

    private AttachmentService CreateSut()
        => new AttachmentService(_attachmentRepo.Object, _formCaseRepo.Object, _storage.Object, _logger.Object);

    private static PdfAttachment ValidPdfAttachment()
        => new PdfAttachment
        {
            Id = 0, 
            FileName = "test.pdf",
            ContentType = "application/pdf",
            SizeBytes = 123,
            UploadedByEmployeeId = 1,
            FormCaseId = 3
        };

    private static Stream DummyPdfStream()
        => new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 dummy"));

    // -------------------------
    // AddAttachmentAsync
    // -------------------------

    [Fact]
    public async Task AddAttachmentAsync_WhenFormCaseIdInvalid_ReturnsFalse()
    {
        var sut = CreateSut();

        var (added, error) = await sut.AddAttachmentAsync(0, ValidPdfAttachment(), DummyPdfStream());

        Assert.False(added);
        Assert.Equal("Invalid FormCaseId", error);

        _formCaseRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _attachmentRepo.Verify(r => r.AddAsync(It.IsAny<PdfAttachment>()), Times.Never);
        _storage.Verify(s => s.SavePdfAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenFormCaseNotFound_ReturnsFalse()
    {
        _formCaseRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync((FormCase?)null);

        var sut = CreateSut();
        var (added, error) = await sut.AddAttachmentAsync(3, ValidPdfAttachment(), DummyPdfStream());

        Assert.False(added);
        Assert.Equal("FormCase not found", error);

        _attachmentRepo.Verify(r => r.AddAsync(It.IsAny<PdfAttachment>()), Times.Never);
        _storage.Verify(s => s.SavePdfAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenNotPdf_ReturnsFalse()
    {
        _formCaseRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new FormCase { Id = 3 });

        var attachment = ValidPdfAttachment();
        attachment.ContentType = "image/png";

        var sut = CreateSut();
        var (added, error) = await sut.AddAttachmentAsync(3, attachment, DummyPdfStream());

        Assert.False(added);
        Assert.Equal("Only PDF files are allowed", error);

        _attachmentRepo.Verify(r => r.AddAsync(It.IsAny<PdfAttachment>()), Times.Never);
        _storage.Verify(s => s.SavePdfAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenSavePdfFails_RollsBack_MetadataDelete()
    {
        _formCaseRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new FormCase { Id = 3 });

        // Metadata Add ok
        _attachmentRepo.Setup(r => r.AddAsync(It.IsAny<PdfAttachment>())).ReturnsAsync(true);

        // File save fails -> must delete metadata
        _storage.Setup(s => s.SavePdfAsync(3, It.IsAny<int>(), It.IsAny<Stream>()))
            .ReturnsAsync((false, "disk error", (string?)null));

        // Repository delete ok
        _attachmentRepo.Setup(r => r.DeleteAsync(It.IsAny<PdfAttachment>())).ReturnsAsync(true);

        var sut = CreateSut();
        var attachment = ValidPdfAttachment();

        var (added, error) = await sut.AddAttachmentAsync(3, attachment, DummyPdfStream());

        Assert.False(added);
        Assert.Equal("disk error", error);

        _attachmentRepo.Verify(r => r.AddAsync(It.Is<PdfAttachment>(a => a == attachment)), Times.Once);
        _storage.Verify(s => s.SavePdfAsync(3, attachment.Id, It.IsAny<Stream>()), Times.Once);
        _attachmentRepo.Verify(r => r.DeleteAsync(It.Is<PdfAttachment>(a => a == attachment)), Times.Once);

        // update storage key must NOT be called when save fails
        _attachmentRepo.Verify(r => r.UpdateStorageKeyAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenUpdateStorageKeyFails_RollsBack_FileAndMetadata()
    {
        _formCaseRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new FormCase { Id = 3 });

        _attachmentRepo.Setup(r => r.AddAsync(It.IsAny<PdfAttachment>())).ReturnsAsync(true);

        // Save ok -> returns storageKey
        _storage.Setup(s => s.SavePdfAsync(3, It.IsAny<int>(), It.IsAny<Stream>()))
            .ReturnsAsync((true, (string?)null, "k/3/att-99.pdf"));

        // Update storage key fails -> rollback file + metadata
        _attachmentRepo.Setup(r => r.UpdateStorageKeyAsync(It.IsAny<int>(), "k/3/att-99.pdf"))
            .ReturnsAsync(false);

        _storage.Setup(s => s.DeleteAsync("k/3/att-99.pdf"))
            .ReturnsAsync((true, (string?)null));

        _attachmentRepo.Setup(r => r.DeleteAsync(It.IsAny<PdfAttachment>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var attachment = ValidPdfAttachment();

        var (added, error) = await sut.AddAttachmentAsync(3, attachment, DummyPdfStream());

        Assert.False(added);
        Assert.Equal("Error while finalizing attachment", error);

        _storage.Verify(s => s.SavePdfAsync(3, attachment.Id, It.IsAny<Stream>()), Times.Once);
        _attachmentRepo.Verify(r => r.UpdateStorageKeyAsync(attachment.Id, "k/3/att-99.pdf"), Times.Once);
        _storage.Verify(s => s.DeleteAsync("k/3/att-99.pdf"), Times.Once);
        _attachmentRepo.Verify(r => r.DeleteAsync(It.Is<PdfAttachment>(a => a == attachment)), Times.Once);
    }

    // -------------------------
    // DownloadAsync
    // -------------------------

    [Fact]
    public async Task DownloadAsync_WhenIdInvalid_ReturnsError()
    {
        var sut = CreateSut();

        var (stream, fileName, contentType, error) = await sut.DownloadAsync(0);

        Assert.Null(stream);
        Assert.Equal("Invalid attachmentId", error);
        _attachmentRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenAttachmentNotFound_ReturnsError()
    {
        _attachmentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((PdfAttachment?)null);

        var sut = CreateSut();
        var result = await sut.DownloadAsync(10);

        Assert.Null(result.stream);
        Assert.Equal("Attachment not found", result.error);
    }

    [Fact]
    public async Task DownloadAsync_WhenStorageKeyMissing_ReturnsError()
    {
        _attachmentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new PdfAttachment
        {
            Id = 10,
            FileName = "a.pdf",
            ContentType = "application/pdf",
            StorageKey = "  "
        });

        var sut = CreateSut();
        var result = await sut.DownloadAsync(10);

        Assert.Null(result.stream);
        Assert.Equal("Attachment has no file reference", result.error);
        _storage.Verify(s => s.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileMissing_ReturnsError()
    {
        _attachmentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new PdfAttachment
        {
            Id = 10,
            FileName = "a.pdf",
            ContentType = "application/pdf",
            StorageKey = "k/a.pdf"
        });

        _storage.Setup(s => s.OpenReadAsync("k/a.pdf"))
            .ReturnsAsync(((Stream?)null, "File not found"));

        var sut = CreateSut();
        var result = await sut.DownloadAsync(10);

        Assert.Null(result.stream);
        Assert.Equal("File not found", result.error);
    }

    [Fact]
    public async Task DownloadAsync_WhenOk_ReturnsStreamAndMetadata()
    {
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        _attachmentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new PdfAttachment
        {
            Id = 10,
            FileName = "a.pdf",
            ContentType = "application/pdf",
            StorageKey = "k/a.pdf"
        });

        _storage.Setup(s => s.OpenReadAsync("k/a.pdf"))
            .ReturnsAsync((ms, (string?)null));

        var sut = CreateSut();
        var (stream, fileName, contentType, error) = await sut.DownloadAsync(10);

        Assert.NotNull(stream);
        Assert.Equal("a.pdf", fileName);
        Assert.Equal("application/pdf", contentType);
        Assert.Null(error);
    }

    // -------------------------
    // DeleteAttachmentAsync
    // -------------------------

    [Fact]
    public async Task DeleteAttachmentAsync_WhenAttachmentNull_ReturnsFalse()
    {
        var sut = CreateSut();

        var (deleted, error) = await sut.DeleteAttachmentAsync(null!);

        Assert.False(deleted);
        Assert.Equal("Attachment must not be null", error);
        _attachmentRepo.Verify(r => r.DeleteAsync(It.IsAny<PdfAttachment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WhenInvalidId_ReturnsFalse()
    {
        var sut = CreateSut();

        var (deleted, error) = await sut.DeleteAttachmentAsync(new PdfAttachment { Id = 0 });

        Assert.False(deleted);
        Assert.Equal("Invalid attachmentId", error);
        _attachmentRepo.Verify(r => r.DeleteAsync(It.IsAny<PdfAttachment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WhenFileDeleteFails_StillDeletesMetadata()
    {
        var attachment = new PdfAttachment { Id = 10, StorageKey = "k/a.pdf" };

        _storage.Setup(s => s.DeleteAsync("k/a.pdf"))
            .ReturnsAsync((false, "no permission"));

        _attachmentRepo.Setup(r => r.DeleteAsync(It.IsAny<PdfAttachment>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var (deleted, error) = await sut.DeleteAttachmentAsync(attachment);

        Assert.True(deleted);
        Assert.Null(error);

        _storage.Verify(s => s.DeleteAsync("k/a.pdf"), Times.Once);
        _attachmentRepo.Verify(r => r.DeleteAsync(It.Is<PdfAttachment>(a => a == attachment)), Times.Once);
    }
}
