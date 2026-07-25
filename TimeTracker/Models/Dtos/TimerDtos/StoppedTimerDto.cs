namespace TimeTracker.Models.Dtos.TimerDtos;

public class StoppedTimerDto
{
    public int TimeEntryId { get; set; }
    public int ProjectId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime StoppedAtUtc { get; set; }
    public long DurationSeconds { get; set; }
    public long ProjectTotalTimeSeconds { get; set; }
}
