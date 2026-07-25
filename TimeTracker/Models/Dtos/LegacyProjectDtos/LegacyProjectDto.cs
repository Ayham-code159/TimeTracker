namespace TimeTracker.Models.Dtos.LegacyProjectDtos;

public class LegacyProjectDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long TotalTimeSeconds { get; set; }
}
