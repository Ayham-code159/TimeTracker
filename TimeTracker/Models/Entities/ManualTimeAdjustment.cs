namespace TimeTracker.Models.Entities;

public class ManualTimeAdjustment
{
    public int Id { get; set; }
    public long DurationSeconds { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
