using CaseFlow.Application.Interfaces;
using CaseFlow.Web.Auth;
using CaseFlow.Web.Dtos.ClarificationDtos;
using CaseFlow.Web.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ClarificationsController : ControllerBase
{
    private readonly IClarificationService _service;

    public ClarificationsController(IClarificationService service) => _service = service;

    [HttpGet("formcases/{formCaseId:int}/clarifications")]
    [ProducesResponseType(typeof(List<ClarificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ClarificationResponseDto>>> GetByFormCase(int formCaseId)
    {
        try
        {
            var messages = await _service.GetByFormCaseAsync(formCaseId);
            var dtos = messages.Select(m => m.ToDto()).ToList();
            return Ok(dtos);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("formcases/{formCaseId:int}/clarifications")]
    [ProducesResponseType(typeof(ClarificationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(int formCaseId, [FromBody] CreateClarificationRequestDto dto)
    {
        int actingEmployeeId;
        try
        {
            actingEmployeeId = User.GetEmployeeId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }

        var (added, error, created) = await _service.AddAsync(actingEmployeeId, formCaseId, dto.Message);
        if (!added || created is null)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetByFormCase), new { formCaseId }, created.ToDto());
    }
}
