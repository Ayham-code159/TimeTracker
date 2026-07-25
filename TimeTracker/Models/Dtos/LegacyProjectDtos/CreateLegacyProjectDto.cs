namespace TimeTracker.Models.Dtos.LegacyProjectDtos;

public class CreateLegacyProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
}
