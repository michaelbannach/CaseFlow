using CaseFlow.Application.Interfaces;
using CaseFlow.Web.Dtos.FormCaseDtos;
using CaseFlow.Web.Mappings;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/formcases")]
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
        var cases = await _formCaseService.GetAllFormCasesAsync();
        var dtos = cases.Select(c => c.ToDto()).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FormCaseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FormCaseResponseDto>> GetById(int id)
    {
        var formCase = await _formCaseService.GetFormCaseByIdAsync(id);
        if (formCase is null)
            return NotFound();

        return Ok(formCase.ToDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(FormCaseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateFormCaseRequestDto dto)
    {
        // actingEmployeeId kommt aus DTO (später aus Identity/Claims)
        if (dto.ActingEmployeeId <= 0)
            return BadRequest(new { error = "ActingEmployeeId is required" });

        var entity = dto.ToEntity();

        var (added, error) = await _formCaseService.CreateFormCaseAsync(dto.ActingEmployeeId, entity);
        if (!added)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
    {
        if (dto.ActingEmployeeId <= 0)
            return BadRequest(new { error = "ActingEmployeeId is required" });

        var (updated, error) = await _formCaseService.UpdateFormCaseStatusAsync(
            dto.ActingEmployeeId,
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
    public async Task<ActionResult> Delete(int id, [FromQuery] int actingEmployeeId)
    {
        if (actingEmployeeId <= 0)
            return BadRequest(new { error = "actingEmployeeId is required" });

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
