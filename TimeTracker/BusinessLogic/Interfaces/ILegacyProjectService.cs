using TimeTracker.Models.Dtos.LegacyProjectDtos;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Interfaces;

public interface ILegacyProjectService
{
    Task<ApiResponse<List<LegacyProjectDto>>> GetAllAsync(string userId, string provider);
    Task<ApiResponse<LegacyProjectDto>> CreateAsync(string userId, string provider, CreateLegacyProjectDto request);
    Task<ApiResponse<LegacyProjectDto>> UpdateAsync(string userId, UpdateLegacyProjectDto request);
    Task<ApiResponse<LegacyProjectDto>> AddTimeAsync(string userId, int projectId, AddManualTimeDto request);
    Task<ApiResponse> DeleteAsync(string userId, int projectId);
}
