namespace TimeTracker.Models.Dtos.AuthDtos;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAtUtc { get; set; }
    public string Username { get; set; } = string.Empty;
}
