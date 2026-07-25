namespace TimeTracker.Models.Entities;

public class TimeEntry
{
    public int Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ResumedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
    public long? DurationSeconds { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
}
