using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Data;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Models.Dtos.TimerDtos;
using TimeTracker.Models.Entities;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProjectDto> _createValidator;
    private readonly IValidator<UpdateProjectDto> _updateValidator;
    private readonly IValidator<ProjectIdDto> _idValidator;
    private readonly IValidator<ProjectNameDto> _nameValidator;
    private readonly IValidator<AddManualTimeDto> _manualTimeValidator;

    public ProjectService(
        ApplicationDbContext context,
        IValidator<CreateProjectDto> createValidator,
        IValidator<UpdateProjectDto> updateValidator,
        IValidator<ProjectIdDto> idValidator,
        IValidator<ProjectNameDto> nameValidator,
        IValidator<AddManualTimeDto> manualTimeValidator,
        IMapper mapper)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _idValidator = idValidator;
        _nameValidator = nameValidator;
        _manualTimeValidator = manualTimeValidator;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ProjectDto>> CreateAsync(
        string userId,
        CreateProjectDto request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(x => x.ErrorMessage).ToList()
            };
        }

        var normalizedName = NormalizeName(request.Name);
        var nameExists = await _context.Projects.AnyAsync(x =>
            x.ApplicationUserId == userId &&
            x.NormalizedName == normalizedName);

        if (nameExists)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "A project with this name already exists."
            };
        }

        var project = _mapper.Map<Project>(request);
        project.NormalizedName = normalizedName;
        project.CreatedAtUtc = DateTime.UtcNow;
        project.ApplicationUserId = userId;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return new ApiResponse<ProjectDto>
        {
            Success = true,
            Message = "Project created successfully.",
            Data = _mapper.Map<ProjectDto>(project)
        };
    }

    public async Task<ApiResponse<ProjectDto>> UpdateAsync(
        string userId,
        UpdateProjectDto request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(x => x.ErrorMessage).ToList()
            };
        }

        var project = await _context.Projects
            .Include(x => x.TimeEntries)
            .Include(x => x.ManualTimeAdjustments)
            .SingleOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.ApplicationUserId == userId);

        if (project is null)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Project was not found."
            };
        }

        var normalizedName = NormalizeName(request.Name);
        var nameExists = await _context.Projects.AnyAsync(x =>
            x.ApplicationUserId == userId &&
            x.NormalizedName == normalizedName &&
            x.Id != request.Id);

        if (nameExists)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "A project with this name already exists."
            };
        }

        _mapper.Map(request, project);
        project.NormalizedName = normalizedName;

        await _context.SaveChangesAsync();

        return new ApiResponse<ProjectDto>
        {
            Success = true,
            Message = "Project updated successfully.",
            Data = _mapper.Map<ProjectDto>(project)
        };
    }

    public async Task<ApiResponse<List<ProjectDto>>> GetAllAsync(string userId)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId)
            .OrderBy(x => x.Name)
            .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new ApiResponse<List<ProjectDto>>
        {
            Success = true,
            Message = "Projects retrieved successfully.",
            Data = projects
        };
    }

    public async Task<ApiResponse<ProjectDto>> GetByNameAsync(
        string userId,
        ProjectNameDto request)
    {
        var validation = await _nameValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(x => x.ErrorMessage).ToList()
            };
        }

        var normalizedName = NormalizeName(request.Name);
        var project = await _context.Projects
            .AsNoTracking()
            .Where(x =>
                x.ApplicationUserId == userId &&
                x.NormalizedName == normalizedName)
            .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();

        if (project is null)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Project was not found."
            };
        }

        return new ApiResponse<ProjectDto>
        {
            Success = true,
            Message = "Project retrieved successfully.",
            Data = project
        };
    }

    public async Task<ApiResponse> DeleteAsync(string userId, ProjectIdDto request)
    {
        var validation = await _idValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(x => x.ErrorMessage).ToList()
            };
        }

        var project = await _context.Projects.SingleOrDefaultAsync(x =>
            x.Id == request.Id &&
            x.ApplicationUserId == userId);

        if (project is null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Project was not found."
            };
        }

        var timerIsRunning = await _context.TimeEntries.AnyAsync(x =>
            x.ProjectId == request.Id &&
            x.ApplicationUserId == userId &&
            x.StoppedAtUtc == null);

        if (timerIsRunning)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Stop the running timer before deleting this project."
            };
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return new ApiResponse
        {
            Success = true,
            Message = "Project deleted successfully."
        };
    }

    public async Task<ApiResponse<ProjectDto>> AddManualTimeAsync(
        string userId,
        int projectId,
        AddManualTimeDto request)
    {
        var validation = await _manualTimeValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(error => error.ErrorMessage).ToList()
            };
        }

        var project = await _context.Projects
            .Include(item => item.TimeEntries)
            .Include(item => item.ManualTimeAdjustments)
            .SingleOrDefaultAsync(item =>
                item.Id == projectId &&
                item.ApplicationUserId == userId);

        if (project is null)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "Project was not found."
            };
        }

        var secondsToAdd = ((long)request.Hours * 60 + request.Minutes) * 60;

        try
        {
            project.ManualTimeSeconds = checked(project.ManualTimeSeconds + secondsToAdd);
        }
        catch (OverflowException)
        {
            return new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "The manual time total is too large."
            };
        }

        project.ManualTimeAdjustments.Add(new ManualTimeAdjustment
        {
            DurationSeconds = secondsToAdd,
            AddedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new ApiResponse<ProjectDto>
        {
            Success = true,
            Message = "Manual time added successfully.",
            Data = _mapper.Map<ProjectDto>(project)
        };
    }

    public async Task<ApiResponse<RunningTimerDto>> StartTimerAsync(
        string userId,
        ProjectIdDto request)
    {
        var validation = await _idValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return new ApiResponse<RunningTimerDto>
            {
                Success = false,
                Message = "Validation Error",
                Errors = validation.Errors.Select(x => x.ErrorMessage).ToList()
            };
        }

        var runningTimer = await _context.TimeEntries
            .AsNoTracking()
            .Include(x => x.Project)
            .SingleOrDefaultAsync(x =>
                x.ApplicationUserId == userId &&
                x.StoppedAtUtc == null);

        if (runningTimer is not null)
        {
            return new ApiResponse<RunningTimerDto>
            {
                Success = false,
                Message = $"A timer is already running for project '{runningTimer.Project.Name}'. Stop it before starting another timer."
            };
        }

        var project = await _context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.ApplicationUserId == userId);

        if (project is null)
        {
            return new ApiResponse<RunningTimerDto>
            {
                Success = false,
                Message = "Project was not found."
            };
        }

        var timeEntry = new TimeEntry
        {
            ProjectId = project.Id,
            ApplicationUserId = userId,
            StartedAtUtc = DateTime.UtcNow
        };

        _context.TimeEntries.Add(timeEntry);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _context.Entry(timeEntry).State = EntityState.Detached;
            return new ApiResponse<RunningTimerDto>
            {
                Success = false,
                Message = "A timer is already running. Stop it before starting another timer."
            };
        }

        return new ApiResponse<RunningTimerDto>
        {
            Success = true,
            Message = "Timer started successfully.",
            Data = _mapper.Map<RunningTimerDto>(timeEntry, options =>
                options.AfterMap((_, destination) =>
                {
                    destination.ProjectName = project.Name;
                    destination.ProjectColor = project.Color;
                }))
        };
    }

    public async Task<ApiResponse<RunningTimerDto>> GetRunningTimerAsync(string userId)
    {
        var timeEntry = await _context.TimeEntries
            .AsNoTracking()
            .Include(x => x.Project)
            .SingleOrDefaultAsync(x =>
                x.ApplicationUserId == userId &&
                x.StoppedAtUtc == null);

        if (timeEntry is null)
        {
            return new ApiResponse<RunningTimerDto>
            {
                Success = false,
                Message = "There is no running timer."
            };
        }

        var activeSince = timeEntry.ResumedAtUtc ?? timeEntry.StartedAtUtc;
        var elapsedSeconds = (timeEntry.DurationSeconds ?? 0) + Math.Max(
            0,
            (long)(DateTime.UtcNow - activeSince).TotalSeconds);

        return new ApiResponse<RunningTimerDto>
        {
            Success = true,
            Message = "Running timer retrieved successfully.",
            Data = _mapper.Map<RunningTimerDto>(timeEntry, options =>
                options.AfterMap((_, destination) =>
                    destination.ElapsedSeconds = elapsedSeconds))
        };
    }

    public async Task<ApiResponse<StoppedTimerDto>> StopTimerAsync(string userId)
    {
        var timeEntry = await _context.TimeEntries.SingleOrDefaultAsync(x =>
            x.ApplicationUserId == userId &&
            x.StoppedAtUtc == null);

        if (timeEntry is null)
        {
            return new ApiResponse<StoppedTimerDto>
            {
                Success = false,
                Message = "There is no running timer to stop."
            };
        }

        var stoppedAtUtc = DateTime.UtcNow;
        var activeSince = timeEntry.ResumedAtUtc ?? timeEntry.StartedAtUtc;
        var activeDurationSeconds = Math.Max(
            0,
            (long)(stoppedAtUtc - activeSince).TotalSeconds);

        timeEntry.StoppedAtUtc = stoppedAtUtc;
        timeEntry.DurationSeconds = (timeEntry.DurationSeconds ?? 0) + activeDurationSeconds;
        timeEntry.ResumedAtUtc = null;
        await _context.SaveChangesAsync();

        var totalTimeSeconds = await _context.TimeEntries
            .Where(x =>
                x.ProjectId == timeEntry.ProjectId &&
                x.ApplicationUserId == userId)
            .SumAsync(x => x.DurationSeconds ?? 0);
        totalTimeSeconds += await _context.Projects
            .Where(project =>
                project.Id == timeEntry.ProjectId &&
                project.ApplicationUserId == userId)
            .Select(project => project.ManualTimeSeconds)
            .SingleAsync();

        return new ApiResponse<StoppedTimerDto>
        {
            Success = true,
            Message = "Timer stopped and saved successfully.",
            Data = _mapper.Map<StoppedTimerDto>(timeEntry, options =>
                options.AfterMap((_, destination) =>
                    destination.ProjectTotalTimeSeconds = totalTimeSeconds))
        };
    }

    public async Task<ApiResponse<List<TimeEntryDto>>> GetAllTimeEntriesAsync(
        string userId)
    {
        var entries = await _context.TimeEntries
            .AsNoTracking()
            .Include(entry => entry.Project)
            .Where(entry => entry.ApplicationUserId == userId)
            .OrderByDescending(entry => entry.StartedAtUtc)
            .ToListAsync();

        var entryDtos = _mapper.Map<List<TimeEntryDto>>(entries);
        var nowUtc = DateTime.UtcNow;

        foreach (var entry in entryDtos.Where(entry => entry.IsRunning))
        {
            var sourceEntry = entries.Single(source => source.Id == entry.Id);
            var activeSince = sourceEntry.ResumedAtUtc ?? sourceEntry.StartedAtUtc;
            entry.DurationSeconds += Math.Max(
                0,
                (long)(nowUtc - activeSince).TotalSeconds);
        }

        return new ApiResponse<List<TimeEntryDto>>
        {
            Success = true,
            Message = "Time entries retrieved successfully.",
            Data = entryDtos
        };
    }

    public async Task<ApiResponse> DeleteTimeEntryAsync(
        string userId,
        int timeEntryId)
    {
        if (timeEntryId <= 0)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "A valid time entry is required."
            };
        }

        var timeEntry = await _context.TimeEntries.SingleOrDefaultAsync(entry =>
            entry.Id == timeEntryId &&
            entry.ApplicationUserId == userId);

        if (timeEntry is null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Time entry was not found."
            };
        }

        if (timeEntry.StoppedAtUtc is null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Stop the running timer before deleting this time entry."
            };
        }

        _context.TimeEntries.Remove(timeEntry);
        await _context.SaveChangesAsync();

        return new ApiResponse
        {
            Success = true,
            Message = "Time entry deleted successfully."
        };
    }

    public async Task<ApiResponse<RunningTimerDto>> ResumeTimerAsync(
        string userId,
        int timeEntryId)
    {
        if (timeEntryId <= 0)
            return TimerFailure("A valid time entry is required.");

        var runningTimer = await _context.TimeEntries
            .AsNoTracking()
            .Include(x => x.Project)
            .SingleOrDefaultAsync(x => x.ApplicationUserId == userId && x.StoppedAtUtc == null);

        if (runningTimer is not null)
            return TimerFailure($"A timer is already running for project '{runningTimer.Project.Name}'. Stop it before continuing another entry.");

        var timeEntry = await _context.TimeEntries
            .Include(x => x.Project)
            .SingleOrDefaultAsync(x => x.Id == timeEntryId && x.ApplicationUserId == userId);

        if (timeEntry is null)
            return TimerFailure("Time entry was not found.");

        timeEntry.ResumedAtUtc = DateTime.UtcNow;
        timeEntry.StoppedAtUtc = null;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            await _context.Entry(timeEntry).ReloadAsync();
            return TimerFailure("A timer is already running. Stop it before continuing another entry.");
        }

        return new ApiResponse<RunningTimerDto>
        {
            Success = true,
            Message = "Time entry continued successfully.",
            Data = _mapper.Map<RunningTimerDto>(timeEntry, options =>
                options.AfterMap((_, destination) =>
                    destination.ElapsedSeconds = timeEntry.DurationSeconds ?? 0))
        };
    }

    private static ApiResponse<RunningTimerDto> TimerFailure(string message) =>
        new() { Success = false, Message = message };

    private static string NormalizeName(string name) =>
        name.Trim().ToUpperInvariant();

}
