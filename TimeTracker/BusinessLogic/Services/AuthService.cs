using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Models.Dtos.AuthDtos;
using TimeTracker.Models.Entities;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IValidator<LoginDto> loginValidator,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _loginValidator = loginValidator;
        _configuration = configuration;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto request)
    {
        var validation = await _loginValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            return new ApiResponse<LoginResponseDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors
                    .Select(error => error.ErrorMessage)
                    .ToList()
            };
        }

        var user = await _userManager.FindByNameAsync(request.Username.Trim());

        if (user is null)
        {
            return LoginFailure();
        }

        if (await _userManager.IsLockedOutAsync(user))
            return LoginFailure();

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return LoginFailure();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var key = GetRequiredSetting("Jwt:Key");
        var expirationMinutes = _configuration.GetValue<int?>(
            "Jwt:ExpirationMinutes") ?? 60;

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new ApiResponse<LoginResponseDto>
        {
            Success = true,
            Message = "Login successful.",
            Data = new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                Username = user.UserName!
            }
        };
    }

    private string GetRequiredSetting(string key) =>
        _configuration[key]
        ?? throw new InvalidOperationException(
            $"Required configuration value '{key}' was not found.");

    private static ApiResponse<LoginResponseDto> LoginFailure() =>
        new()
        {
            Success = false,
            Message = "Invalid username or password."
        };
}
