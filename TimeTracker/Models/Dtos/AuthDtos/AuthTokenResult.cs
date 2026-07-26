namespace TimeTracker.Models.Dtos.AuthDtos;

public class AuthTokenResult
{
    public LoginResponseDto Response { get; set; } = null!;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
