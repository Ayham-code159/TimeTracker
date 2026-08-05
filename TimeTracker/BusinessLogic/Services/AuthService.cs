using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Data;
using TimeTracker.Models.Dtos.AuthDtos;
using TimeTracker.Models.Entities;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private const string RotationReason = "Rotated";
    private const string ReuseReason = "Refresh token reuse detected";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _database;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IValidator<LoginDto> loginValidator,
        IConfiguration configuration,
        ApplicationDbContext database)
    {
        _userManager = userManager;
        _loginValidator = loginValidator;
        _configuration = configuration;
        _database = database;
    }

    public async Task<ApiResponse<AuthTokenResult>> LoginAsync(LoginDto request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Failure(
                "Validation Error",
                validation.Errors.Select(error => error.ErrorMessage).ToList());
        }

        var user = await _userManager.FindByNameAsync(request.Username.Trim());
        if (user is null || await _userManager.IsLockedOutAsync(user))
            return Failure();

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return Failure();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var now = DateTime.UtcNow;
        await DeleteOldTokensAsync(now);
        var familyExpiresAtUtc = now.AddDays(GetPositiveSetting(
            "RefreshToken:AbsoluteExpirationDays",
            14));
        var refreshTokenValue = GenerateRefreshToken();
        var refreshToken = CreateRefreshToken(
            user,
            refreshTokenValue,
            Guid.NewGuid(),
            now,
            familyExpiresAtUtc);

        _database.RefreshTokens.Add(refreshToken);
        await _database.SaveChangesAsync();

        return Success(
            await CreateTokenResultAsync(user, refreshTokenValue, refreshToken.ExpiresAtUtc));
    }

    public async Task<ApiResponse<AuthTokenResult>> RefreshAsync(string refreshTokenValue)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
            return Failure("Refresh token is missing.");

        var tokenHash = HashToken(refreshTokenValue);
        var currentToken = await _database.RefreshTokens
            .Include(token => token.ApplicationUser)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (currentToken is null)
            return Failure("Refresh token is invalid.");

        var now = DateTime.UtcNow;
        if (currentToken.RevokedAtUtc is not null)
        {
            var concurrentResult = await TryCompleteConcurrentRotationAsync(
                currentToken,
                refreshTokenValue,
                now);
            if (concurrentResult is not null)
                return Success(concurrentResult);

            if (currentToken.RevocationReason == RotationReason)
                await RevokeFamilyAsync(currentToken.FamilyId, now, ReuseReason);

            return Failure("Refresh token is invalid.");
        }

        if (currentToken.ExpiresAtUtc <= now ||
            currentToken.FamilyExpiresAtUtc <= now)
        {
            await RevokeFamilyAsync(currentToken.FamilyId, now, "Expired");
            return Failure("Refresh token has expired.");
        }

        var user = currentToken.ApplicationUser;
        if (await _userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(
                currentToken.FamilyId,
                now,
                "User locked out");
            return Failure("Refresh token is invalid.");
        }

        var currentSecurityStamp = await _userManager.GetSecurityStampAsync(user);
        if (!string.Equals(
                currentToken.SecurityStamp,
                currentSecurityStamp,
                StringComparison.Ordinal))
        {
            await RevokeFamilyAsync(
                currentToken.FamilyId,
                now,
                "User security stamp changed");
            return Failure("Refresh token is invalid.");
        }

        var nextTokenValue = DeriveNextRefreshToken(refreshTokenValue);
        var nextTokenHash = HashToken(nextTokenValue);

        await using var transaction = await _database.Database.BeginTransactionAsync();
        var updated = await _database.RefreshTokens
            .Where(token =>
                token.Id == currentToken.Id &&
                token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAtUtc, now)
                .SetProperty(token => token.RevocationReason, RotationReason)
                .SetProperty(token => token.ReplacedByTokenHash, nextTokenHash));

        if (updated != 1)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            _database.ChangeTracker.Clear();

            var concurrentlyRotatedToken = await _database.RefreshTokens
                .Include(token => token.ApplicationUser)
                .SingleAsync(token => token.Id == currentToken.Id);
            var concurrentResult = await TryCompleteConcurrentRotationAsync(
                concurrentlyRotatedToken,
                refreshTokenValue,
                now);
            if (concurrentResult is not null)
                return Success(concurrentResult);

            await RevokeFamilyAsync(
                concurrentlyRotatedToken.FamilyId,
                now,
                ReuseReason);
            return Failure("Refresh token is invalid.");
        }

        var nextToken = CreateRefreshToken(
            user,
            nextTokenValue,
            currentToken.FamilyId,
            now,
            currentToken.FamilyExpiresAtUtc);
        _database.RefreshTokens.Add(nextToken);
        await _database.SaveChangesAsync();
        await transaction.CommitAsync();

        return Success(
            await CreateTokenResultAsync(user, nextTokenValue, nextToken.ExpiresAtUtc));
    }

    public async Task RevokeAsync(string refreshTokenValue)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
            return;

        var tokenHash = HashToken(refreshTokenValue);
        var token = await _database.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash);
        if (token is null)
            return;

        await RevokeFamilyAsync(token.FamilyId, DateTime.UtcNow, "Logged out");
    }

    private async Task<AuthTokenResult> CreateTokenResultAsync(
        ApplicationUser user,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var key = GetRequiredSetting("Jwt:Key");
        var expirationMinutes = GetPositiveSetting("Jwt:ExpirationMinutes", 15);
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
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthTokenResult
        {
            Response = new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                Username = user.UserName!
            },
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    private RefreshToken CreateRefreshToken(
        ApplicationUser user,
        string tokenValue,
        Guid familyId,
        DateTime now,
        DateTime familyExpiresAtUtc)
    {
        var idleExpiresAtUtc = now.AddDays(GetPositiveSetting(
            "RefreshToken:IdleExpirationDays",
            7));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = HashToken(tokenValue),
            FamilyId = familyId,
            ApplicationUserId = user.Id,
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            CreatedAtUtc = now,
            ExpiresAtUtc = idleExpiresAtUtc < familyExpiresAtUtc
                ? idleExpiresAtUtc
                : familyExpiresAtUtc,
            FamilyExpiresAtUtc = familyExpiresAtUtc
        };
    }

    private Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAtUtc,
        string reason) =>
        _database.RefreshTokens
            .Where(token =>
                token.FamilyId == familyId &&
                token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAtUtc, revokedAtUtc)
                .SetProperty(token => token.RevocationReason, reason));

    private Task<int> DeleteOldTokensAsync(DateTime now) =>
        _database.RefreshTokens
            .Where(token =>
                token.FamilyExpiresAtUtc < now.AddDays(-30))
            .ExecuteDeleteAsync();

    private async Task<AuthTokenResult?> TryCompleteConcurrentRotationAsync(
        RefreshToken rotatedToken,
        string presentedTokenValue,
        DateTime now)
    {
        if (rotatedToken.RevocationReason != RotationReason ||
            rotatedToken.RevokedAtUtc is null ||
            string.IsNullOrWhiteSpace(rotatedToken.ReplacedByTokenHash))
            return null;

        var graceSeconds = GetPositiveSetting(
            "RefreshToken:RotationGraceSeconds",
            30);
        if (now - rotatedToken.RevokedAtUtc.Value >
            TimeSpan.FromSeconds(graceSeconds))
            return null;

        var replacementTokenValue = DeriveNextRefreshToken(presentedTokenValue);
        var replacementTokenHash = HashToken(replacementTokenValue);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(rotatedToken.ReplacedByTokenHash),
                Convert.FromHexString(replacementTokenHash)))
            return null;

        var replacementToken = await _database.RefreshTokens
            .Include(token => token.ApplicationUser)
            .SingleOrDefaultAsync(token =>
                token.TokenHash == replacementTokenHash &&
                token.FamilyId == rotatedToken.FamilyId &&
                token.RevokedAtUtc == null);
        if (replacementToken is null ||
            replacementToken.ExpiresAtUtc <= now ||
            replacementToken.FamilyExpiresAtUtc <= now)
            return null;

        return await CreateTokenResultAsync(
            replacementToken.ApplicationUser,
            replacementTokenValue,
            replacementToken.ExpiresAtUtc);
    }

    private static string GenerateRefreshToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private string DeriveNextRefreshToken(string currentToken)
    {
        var key = Encoding.UTF8.GetBytes(GetRequiredSetting("Jwt:Key"));
        var input = Encoding.UTF8.GetBytes(
            $"TimeTracker.RefreshToken.Rotation.v1:{currentToken}");
        return Base64UrlEncoder.Encode(HMACSHA512.HashData(key, input));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string GetRequiredSetting(string key) =>
        _configuration[key]
        ?? throw new InvalidOperationException(
            $"Required configuration value '{key}' was not found.");

    private int GetPositiveSetting(string key, int defaultValue)
    {
        var value = _configuration.GetValue<int?>(key) ?? defaultValue;
        if (value <= 0)
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be greater than zero.");
        return value;
    }

    private static ApiResponse<AuthTokenResult> Success(AuthTokenResult result) =>
        new()
        {
            Success = true,
            Message = "Authentication successful.",
            Data = result
        };

    private static ApiResponse<AuthTokenResult> Failure(
        string message = "Invalid username or password.",
        List<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? []
        };
}
