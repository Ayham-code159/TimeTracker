using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Models.Entities;

namespace TimeTracker.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ManualTimeAdjustment> ManualTimeAdjustments =>
        Set<ManualTimeAdjustment>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(7).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ManualTimeSeconds).IsRequired();

            entity.HasIndex(x => new { x.ApplicationUserId, x.NormalizedName })
                .IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Projects_Color_Hex",
                    "\"Color\" ~ '^#[0-9A-Fa-f]{6}$'");
                table.HasCheckConstraint(
                    "CK_Projects_ManualTime_NonNegative",
                    "\"ManualTimeSeconds\" >= 0");
            });

            entity.HasOne(x => x.ApplicationUser)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TimeEntry>(entity =>
        {
            entity.Property(x => x.StartedAtUtc).IsRequired();

            entity.HasIndex(x => x.ApplicationUserId)
                .IsUnique()
                .HasFilter("\"StoppedAtUtc\" IS NULL");

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_TimeEntries_Duration_NonNegative",
                    "\"DurationSeconds\" IS NULL OR \"DurationSeconds\" >= 0"));

            entity.HasOne(x => x.Project)
                .WithMany(x => x.TimeEntries)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany(x => x.TimeEntries)
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ManualTimeAdjustment>(entity =>
        {
            entity.Property(adjustment => adjustment.DurationSeconds).IsRequired();
            entity.Property(adjustment => adjustment.AddedAtUtc).IsRequired();

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_ManualTimeAdjustments_Duration_Positive",
                    "\"DurationSeconds\" > 0"));

            entity.HasOne(adjustment => adjustment.Project)
                .WithMany(project => project.ManualTimeAdjustments)
                .HasForeignKey(adjustment => adjustment.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
