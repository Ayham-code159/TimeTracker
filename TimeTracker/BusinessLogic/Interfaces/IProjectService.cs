using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Models.Dtos.TimerDtos;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Interfaces;

public interface IProjectService
{
    Task<ApiResponse<ProjectDto>> CreateAsync(string userId, CreateProjectDto request);
    Task<ApiResponse<ProjectDto>> UpdateAsync(string userId, UpdateProjectDto request);
    Task<ApiResponse<List<ProjectDto>>> GetAllAsync(string userId);
    Task<ApiResponse<ProjectDto>> GetByNameAsync(string userId, ProjectNameDto request);
    Task<ApiResponse> DeleteAsync(string userId, ProjectIdDto request);
    Task<ApiResponse<ProjectDto>> AddManualTimeAsync(string userId, int projectId, AddManualTimeDto request);
    Task<ApiResponse<RunningTimerDto>> StartTimerAsync(string userId, ProjectIdDto request);
    Task<ApiResponse<RunningTimerDto>> ResumeTimerAsync(string userId, int timeEntryId);
    Task<ApiResponse<RunningTimerDto>> GetRunningTimerAsync(string userId);
    Task<ApiResponse<StoppedTimerDto>> StopTimerAsync(string userId);
    Task<ApiResponse<List<TimeEntryDto>>> GetAllTimeEntriesAsync(string userId);
    Task<ApiResponse> DeleteTimeEntryAsync(string userId, int timeEntryId);
}
