using Microsoft.AspNetCore.Identity;

namespace TimeTracker.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
