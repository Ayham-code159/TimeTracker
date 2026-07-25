using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Models.Dtos.AuthDtos;

namespace TimeTracker.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var response = await _authService.LoginAsync(request);
        return response.Success ? Ok(response) : Unauthorized(response);
    }
}
