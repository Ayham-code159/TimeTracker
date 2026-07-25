namespace TimeTracker.Models.Dtos.ProjectDtos;

public class ManualTimeAdjustmentDto
{
    public long DurationSeconds { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
