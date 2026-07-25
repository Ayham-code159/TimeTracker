using TimeTracker.Models.Dtos.AuthDtos;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto request);
}
