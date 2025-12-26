using CaseFlow.Application.Interfaces;
using CaseFlow.Domain.Models;
using CaseFlow.Web.Auth;
using CaseFlow.Web.Dtos.PdfAttachmentDtos;
using CaseFlow.Web.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet("formcases/{formCaseId:int}/attachments")]
    [ProducesResponseType(typeof(List<AttachmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AttachmentResponseDto>>> GetByFormCase(int formCaseId)
    {
        var attachments = await _attachmentService.GetAttachmentsByFormCaseAsync(formCaseId);
        var dtos = attachments.Select(a => a.ToDto()).ToList();
        return Ok(dtos);
    }

    [HttpPost("formcases/{formCaseId:int}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(
        int formCaseId,
        [FromForm] UploadAttachmentRequestDto dto)
    {
        if (dto.File is null || dto.File.Length <= 0)
            return BadRequest(new { error = "File is missing" });

        int employeeId;
        try
        {
            employeeId = User.GetEmployeeId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }

        var attachment = new PdfAttachment
        {
            FileName = dto.File.FileName,
            ContentType = dto.File.ContentType,
            SizeBytes = dto.File.Length,
            UploadedByEmployeeId = employeeId
        };

        await using var stream = dto.File.OpenReadStream();

        var (added, error) =
            await _attachmentService.AddAttachmentAsync(formCaseId, attachment, stream);

        if (!added)
            return BadRequest(new { error });

        return CreatedAtAction(
            nameof(Download),
            new { id = attachment.Id },
            attachment.ToDto());
    }

    [HttpGet("attachments/{id:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id)
    {
        var (stream, fileName, contentType, error) = await _attachmentService.DownloadAsync(id);

        if (stream is null)
            return NotFound(new { error });

        return File(stream, contentType ?? "application/pdf", fileName ?? "attachment.pdf");
    }

    [HttpDelete("attachments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _attachmentService.GetAttachmentByIdAsync(id);
        if (existing is null)
            return NotFound();

        var (deleted, error) = await _attachmentService.DeleteAttachmentAsync(existing);
        if (!deleted)
            return BadRequest(new { error });

        return NoContent();
    }
}
