namespace TimeTracker.Models.Dtos.TimerDtos;

public class RunningTimerDto
{
    public int TimeEntryId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectColor { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public long ElapsedSeconds { get; set; }
}
