namespace TimeTracker.Models.Dtos.TimerDtos;

public class TimeEntryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectColor { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
    public long DurationSeconds { get; set; }
    public bool IsRunning { get; set; }
}
