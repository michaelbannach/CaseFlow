using CaseFlow.Application.Interfaces;
using CaseFlow.Web.Dtos.FormCaseDtos;
using CaseFlow.Web.Mappings;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<List<FormCaseResponseDto>>> GetAll()
    {
        var cases = await _formCaseService.GetAllFormCasesAsync();
        var dtos = cases.Select(c => c.ToDto()).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FormCaseResponseDto>> GetById(int id)
    {
        var formCase = await _formCaseService.GetFormCaseByIdAsync(id);
        if (formCase is null)
            return NotFound();

        return Ok(formCase.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateFormCaseRequestDto dto)
    {
        var entity = dto.ToEntity();

        var (added, error) = await _formCaseService.CreateFormCaseAsync(entity);
        if (!added)
            return BadRequest(new { error });

        
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
    {
        var (updated, error) = await _formCaseService.UpdateFormCaseStatusAsync(id, dto.NewStatus);
        if (!updated)
            return BadRequest(new { error });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _formCaseService.GetFormCaseByIdAsync(id);
        if (existing is null)
            return NotFound();

        var (deleted, error) = await _formCaseService.DeleteFormCaseAsync(existing);
        if (!deleted)
            return BadRequest(new { error });

        return NoContent();
    }
}