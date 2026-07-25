using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Models.Dtos.LegacyProjectDtos;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Security;

namespace TimeTracker.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/legacy-projects")]
public class LegacyProjectController : ControllerBase
{
    private readonly ILegacyProjectService _service;

    public LegacyProjectController(ILegacyProjectService service) => _service = service;

    [HttpGet("{provider}")]
    public async Task<IActionResult> GetAll(string provider)
    {
        var response = await _service.GetAllAsync(GetUserId(), provider);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Create(string provider, CreateLegacyProjectDto request)
    {
        var response = await _service.CreateAsync(GetUserId(), provider, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:int}/time")]
    public async Task<IActionResult> AddTime(int id, AddManualTimeDto request)
    {
        var response = await _service.AddTimeAsync(GetUserId(), id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateLegacyProjectDto request)
    {
        request.Id = id;
        var response = await _service.UpdateAsync(GetUserId(), request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _service.DeleteAsync(GetUserId(), id);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Authenticated user ID was not found.");
}
