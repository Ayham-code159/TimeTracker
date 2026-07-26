using TimeTracker.Models.Dtos.AuthDtos;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthTokenResult>> LoginAsync(LoginDto request);
    Task<ApiResponse<AuthTokenResult>> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}
