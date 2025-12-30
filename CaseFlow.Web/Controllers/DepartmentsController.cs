using CaseFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.Web.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;

    public DepartmentsController(IDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _service.GetAllAsync();
        return Ok(departments.Select(d => new { id = d.Id, name = d.Name }));
    }
}