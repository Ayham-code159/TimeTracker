using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Models.Dtos.AuthDtos;

namespace TimeTracker.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var response = await _authService.LoginAsync(request);
        if (!response.Success)
            return Unauthorized(response);

        Response.Cookies.Append(
            "TimeTrackerAuth",
            response.Data!.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = response.Data.ExpiresAtUtc,
                IsEssential = true,
                Path = "/"
            });

        return Ok(response);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            "TimeTrackerAuth",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        return Ok(new
        {
            success = true,
            data = new { username }
        });
    }
}
