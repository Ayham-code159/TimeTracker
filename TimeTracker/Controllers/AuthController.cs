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
    private const string RefreshCookieName = "__Secure-TimeTracker.RefreshToken";
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        if (!HasAllowedOrigin())
            return Forbid();

        var response = await _authService.LoginAsync(request);
        if (!response.Success || response.Data is null)
            return Unauthorized(response);

        SetRefreshCookie(
            response.Data.RefreshToken,
            response.Data.RefreshTokenExpiresAtUtc);
        return Ok(ToPublicResponse(response.Data.Response, "Login successful."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!HasAllowedOrigin())
            return Forbid();

        if (!Request.Cookies.TryGetValue(
                RefreshCookieName,
                out var refreshToken))
        {
            DeleteRefreshCookie();
            return Unauthorized();
        }

        var response = await _authService.RefreshAsync(refreshToken);
        if (!response.Success || response.Data is null)
        {
            DeleteRefreshCookie();
            return Unauthorized();
        }

        SetRefreshCookie(
            response.Data.RefreshToken,
            response.Data.RefreshTokenExpiresAtUtc);
        return Ok(ToPublicResponse(
            response.Data.Response,
            "Token refreshed."));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        if (!HasAllowedOrigin())
            return Forbid();

        if (Request.Cookies.TryGetValue(
                RefreshCookieName,
                out var refreshToken))
        {
            await _authService.RevokeAsync(refreshToken);
        }

        DeleteRefreshCookie();
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

    private object ToPublicResponse(
        LoginResponseDto token,
        string message) =>
        new
        {
            success = true,
            message,
            data = token
        };

    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            token,
            CreateCookieOptions(expiresAtUtc));
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(
            RefreshCookieName,
            CreateCookieOptions(DateTime.UnixEpoch));
    }

    private CookieOptions CreateCookieOptions(DateTime expiresAtUtc) =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = new DateTimeOffset(expiresAtUtc)
        };

    private bool HasAllowedOrigin()
    {
        var allowedOrigins = _configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0)
            return false;

        var origin = Request.Headers.Origin.ToString();
        return allowedOrigins.Any(allowed =>
            string.Equals(
                allowed.TrimEnd('/'),
                origin.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));
    }
}
