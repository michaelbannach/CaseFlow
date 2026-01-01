using CaseFlow.Application.Interfaces;
using CaseFlow.Web.Auth;
using CaseFlow.Web.Dtos.FormCaseDtos;
using CaseFlow.Web.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/formcases")]
[Authorize]
public class FormCasesController : ControllerBase
{
    private readonly IFormCaseService _formCaseService;

    public FormCasesController(IFormCaseService formCaseService)
    {
        _formCaseService = formCaseService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<FormCaseResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FormCaseResponseDto>>> GetAll()
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

        var cases = await _formCaseService.GetAllVisibleFormCasesAsync(actingEmployeeId);
        var dtos = cases.Select(c => c.ToDto()).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FormCaseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FormCaseResponseDto>> GetById(int id)
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

        var formCase = await _formCaseService
            .GetVisibleFormCaseByIdAsync(actingEmployeeId, id);

        if (formCase is null)
            return NotFound();

        return Ok(formCase.ToDto());
    }


    [HttpPost]
    [ProducesResponseType(typeof(FormCaseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Create([FromBody] CreateFormCaseRequestDto dto)
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

        var entity = dto.ToEntity();

        var (added, error) = await _formCaseService.CreateFormCaseAsync(actingEmployeeId, entity);
        if (!added)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
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

        var (updated, error) = await _formCaseService.UpdateFormCaseStatusAsync(
            actingEmployeeId,
            id,
            dto.NewStatus);

        if (!updated)
        {
            if (string.Equals(error, "FormCase not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error });

            return BadRequest(new { error });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete(int id)
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

        var (deleted, error) = await _formCaseService.DeleteFormCaseAsync(actingEmployeeId, id);

        if (!deleted)
        {
            if (string.Equals(error, "FormCase not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error });

            return BadRequest(new { error });
        }

        return NoContent();
    }
}
