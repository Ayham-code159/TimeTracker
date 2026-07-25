using AutoMapper;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Models.Dtos.TimerDtos;
using TimeTracker.Models.Entities;

namespace TimeTracker.MappingProfiles;

public class ProjectMappingProfile : Profile
{
    public ProjectMappingProfile()
    {
        CreateMap<CreateProjectDto, Project>()
            .ForMember(
                destination => destination.Color,
                options => options.MapFrom(source => source.Color.ToUpperInvariant()))
            .ForMember(
                destination => destination.Description,
                options => options.MapFrom(source =>
                    string.IsNullOrWhiteSpace(source.Description)
                        ? null
                        : source.Description.Trim()))
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.NormalizedName, options => options.Ignore())
            .ForMember(destination => destination.CreatedAtUtc, options => options.Ignore())
            .ForMember(destination => destination.ApplicationUserId, options => options.Ignore())
            .ForMember(destination => destination.ApplicationUser, options => options.Ignore())
            .ForMember(destination => destination.TimeEntries, options => options.Ignore())
            .ForMember(destination => destination.ManualTimeAdjustments, options => options.Ignore());

        CreateMap<UpdateProjectDto, Project>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(
                destination => destination.Color,
                options => options.MapFrom(source => source.Color.ToUpperInvariant()))
            .ForMember(
                destination => destination.Description,
                options => options.MapFrom(source =>
                    string.IsNullOrWhiteSpace(source.Description)
                        ? null
                        : source.Description.Trim()))
            .ForMember(destination => destination.NormalizedName, options => options.Ignore())
            .ForMember(destination => destination.CreatedAtUtc, options => options.Ignore())
            .ForMember(destination => destination.ApplicationUserId, options => options.Ignore())
            .ForMember(destination => destination.ApplicationUser, options => options.Ignore())
            .ForMember(destination => destination.TimeEntries, options => options.Ignore())
            .ForMember(destination => destination.ManualTimeAdjustments, options => options.Ignore());

        CreateMap<Project, ProjectDto>()
            .ForMember(
                destination => destination.TotalTimeSeconds,
                options => options.MapFrom(source =>
                    source.ManualTimeSeconds +
                    source.TimeEntries.Sum(entry => entry.DurationSeconds ?? 0)));

        CreateMap<ManualTimeAdjustment, ManualTimeAdjustmentDto>();

        CreateMap<TimeEntry, RunningTimerDto>()
            .ForMember(
                destination => destination.TimeEntryId,
                options => options.MapFrom(source => source.Id))
            .ForMember(
                destination => destination.ProjectName,
                options => options.MapFrom(source => source.Project.Name))
            .ForMember(
                destination => destination.ProjectColor,
                options => options.MapFrom(source => source.Project.Color))
            .ForMember(destination => destination.ElapsedSeconds, options => options.Ignore());

        CreateMap<TimeEntry, StoppedTimerDto>()
            .ForMember(
                destination => destination.TimeEntryId,
                options => options.MapFrom(source => source.Id))
            .ForMember(
                destination => destination.StoppedAtUtc,
                options => options.MapFrom(source => source.StoppedAtUtc!.Value))
            .ForMember(
                destination => destination.DurationSeconds,
                options => options.MapFrom(source => source.DurationSeconds ?? 0))
            .ForMember(
                destination => destination.ProjectTotalTimeSeconds,
                options => options.Ignore());

        CreateMap<TimeEntry, TimeEntryDto>()
            .ForMember(
                destination => destination.ProjectName,
                options => options.MapFrom(source => source.Project.Name))
            .ForMember(
                destination => destination.ProjectColor,
                options => options.MapFrom(source => source.Project.Color))
            .ForMember(
                destination => destination.DurationSeconds,
                options => options.MapFrom(source => source.DurationSeconds ?? 0))
            .ForMember(
                destination => destination.IsRunning,
                options => options.MapFrom(source => source.StoppedAtUtc == null));
    }
}
