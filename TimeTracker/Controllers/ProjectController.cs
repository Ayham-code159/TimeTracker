using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Models.Dtos.ProjectDtos;

namespace TimeTracker.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectDto request)
    {
        var response = await _projectService.CreateAsync(GetUserId(), request);
        return response.Success
            ? CreatedAtAction(nameof(GetByName), new { name = response.Data!.Name }, response)
            : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProjectDto request)
    {
        request.Id = id;
        var response = await _projectService.UpdateAsync(GetUserId(), request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _projectService.GetAllAsync(GetUserId());
        return Ok(response);
    }

    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        var response = await _projectService.GetByNameAsync(
            GetUserId(),
            new ProjectNameDto { Name = name });

        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _projectService.DeleteAsync(
            GetUserId(),
            new ProjectIdDto { Id = id });

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:int}/manual-time")]
    public async Task<IActionResult> AddManualTime(
        int id,
        AddManualTimeDto request)
    {
        var response = await _projectService.AddManualTimeAsync(
            GetUserId(),
            id,
            request);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:int}/timer/start")]
    public async Task<IActionResult> StartTimer(int id)
    {
        var response = await _projectService.StartTimerAsync(
            GetUserId(),
            new ProjectIdDto { Id = id });

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("timer/running")]
    public async Task<IActionResult> GetRunningTimer()
    {
        var response = await _projectService.GetRunningTimerAsync(GetUserId());
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost("time-entries/{timeEntryId:int}/resume")]
    public async Task<IActionResult> ResumeTimer(int timeEntryId)
    {
        var response = await _projectService.ResumeTimerAsync(GetUserId(), timeEntryId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("timer/stop")]
    public async Task<IActionResult> StopTimer()
    {
        var response = await _projectService.StopTimerAsync(GetUserId());
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("time-entries")]
    public async Task<IActionResult> GetAllTimeEntries()
    {
        var response = await _projectService.GetAllTimeEntriesAsync(GetUserId());
        return Ok(response);
    }

    [HttpDelete("time-entries/{timeEntryId:int}")]
    public async Task<IActionResult> DeleteTimeEntry(int timeEntryId)
    {
        var response = await _projectService.DeleteTimeEntryAsync(
            GetUserId(),
            timeEntryId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Authenticated user ID was not found.");
}
