namespace TimeTracker.Models.Dtos.ProjectDtos;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long TotalTimeSeconds { get; set; }
    public long ManualTimeSeconds { get; set; }
    public List<ManualTimeAdjustmentDto> ManualTimeAdjustments { get; set; } = [];
}
