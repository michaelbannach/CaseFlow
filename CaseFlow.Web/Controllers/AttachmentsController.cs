using CaseFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var (stream, fileName, contentType, error) =
            await _attachmentService.DownloadAsync(id);

        if (stream is null)
            return NotFound(new { error });

        return File(stream, contentType ?? "application/pdf", fileName);
    }
}