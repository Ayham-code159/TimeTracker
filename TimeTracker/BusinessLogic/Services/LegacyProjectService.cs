using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TimeTracker.BusinessLogic.Interfaces;
using TimeTracker.Data;
using TimeTracker.Models.Dtos.LegacyProjectDtos;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Models.Entities;
using TimeTracker.Responses;

namespace TimeTracker.BusinessLogic.Services;

public class LegacyProjectService : ILegacyProjectService
{
    public const string Clockify = "Clockify";
    public const string TogglTrack = "TogglTrack";
    private static readonly string[] Providers = [Clockify, TogglTrack];

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateLegacyProjectDto> _createValidator;
    private readonly IValidator<UpdateLegacyProjectDto> _updateValidator;
    private readonly IValidator<AddManualTimeDto> _timeValidator;

    public LegacyProjectService(
        ApplicationDbContext context,
        IValidator<CreateLegacyProjectDto> createValidator,
        IValidator<UpdateLegacyProjectDto> updateValidator,
        IValidator<AddManualTimeDto> timeValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _timeValidator = timeValidator;
    }

    public async Task<ApiResponse<LegacyProjectDto>> UpdateAsync(string userId, UpdateLegacyProjectDto request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return new ApiResponse<LegacyProjectDto> { Success = false, Message = "Validation Error", Errors = validation.Errors.Select(error => error.ErrorMessage).ToList() };

        var project = await _context.LegacyProjects.SingleOrDefaultAsync(project =>
            project.Id == request.Id && project.ApplicationUserId == userId);
        if (project is null)
            return Failure<LegacyProjectDto>("Legacy project was not found.");

        var normalizedName = request.Name.Trim().ToUpperInvariant();
        if (await _context.LegacyProjects.AnyAsync(item =>
            item.ApplicationUserId == userId &&
            item.Provider == project.Provider &&
            item.NormalizedName == normalizedName &&
            item.Id != project.Id))
            return Failure<LegacyProjectDto>("A legacy project with this name already exists for this provider.");

        project.Name = request.Name.Trim();
        project.NormalizedName = normalizedName;
        project.Color = request.Color.ToUpperInvariant();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await _context.SaveChangesAsync();

        return new ApiResponse<LegacyProjectDto> { Success = true, Message = "Legacy project updated successfully.", Data = ToDto(project) };
    }

    public async Task<ApiResponse<List<LegacyProjectDto>>> GetAllAsync(string userId, string provider)
    {
        var normalizedProvider = NormalizeProvider(provider);
        if (normalizedProvider is null)
            return Failure<List<LegacyProjectDto>>("Provider must be Clockify or TogglTrack.");

        var entities = await _context.LegacyProjects.AsNoTracking()
            .Where(project => project.ApplicationUserId == userId && project.Provider == normalizedProvider)
            .OrderBy(project => project.Name)
            .ToListAsync();
        var projects = entities.Select(ToDto).ToList();

        return new ApiResponse<List<LegacyProjectDto>> { Success = true, Message = "Legacy projects retrieved successfully.", Data = projects };
    }

    public async Task<ApiResponse<LegacyProjectDto>> CreateAsync(string userId, string provider, CreateLegacyProjectDto request)
    {
        var normalizedProvider = NormalizeProvider(provider);
        if (normalizedProvider is null)
            return Failure<LegacyProjectDto>("Provider must be Clockify or TogglTrack.");

        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return new ApiResponse<LegacyProjectDto> { Success = false, Message = "Validation Error", Errors = validation.Errors.Select(error => error.ErrorMessage).ToList() };

        var normalizedName = request.Name.Trim().ToUpperInvariant();
        if (await _context.LegacyProjects.AnyAsync(project =>
            project.ApplicationUserId == userId &&
            project.Provider == normalizedProvider &&
            project.NormalizedName == normalizedName))
            return Failure<LegacyProjectDto>("A legacy project with this name already exists for this provider.");

        var project = new LegacyProject
        {
            Provider = normalizedProvider,
            Name = request.Name.Trim(),
            NormalizedName = normalizedName,
            Color = request.Color.ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            ApplicationUserId = userId
        };
        _context.LegacyProjects.Add(project);
        await _context.SaveChangesAsync();

        return new ApiResponse<LegacyProjectDto> { Success = true, Message = "Legacy project created successfully.", Data = ToDto(project) };
    }

    public async Task<ApiResponse<LegacyProjectDto>> AddTimeAsync(string userId, int projectId, AddManualTimeDto request)
    {
        var validation = await _timeValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return new ApiResponse<LegacyProjectDto> { Success = false, Message = "Validation Error", Errors = validation.Errors.Select(error => error.ErrorMessage).ToList() };

        var secondsToAdd = ((long)request.Hours * 60 + request.Minutes) * 60;
        var affected = await _context.LegacyProjects
            .Where(project =>
                project.Id == projectId &&
                project.ApplicationUserId == userId &&
                project.TotalTimeSeconds <= long.MaxValue - secondsToAdd)
            .ExecuteUpdateAsync(update => update.SetProperty(
                project => project.TotalTimeSeconds,
                project => project.TotalTimeSeconds + secondsToAdd));
        if (affected == 0)
        {
            var exists = await _context.LegacyProjects.AnyAsync(project =>
                project.Id == projectId && project.ApplicationUserId == userId);
            return Failure<LegacyProjectDto>(exists
                ? "The legacy project time total is too large."
                : "Legacy project was not found.");
        }

        var project = await _context.LegacyProjects.AsNoTracking().SingleAsync(project =>
            project.Id == projectId && project.ApplicationUserId == userId);
        return new ApiResponse<LegacyProjectDto> { Success = true, Message = "Legacy time added successfully.", Data = ToDto(project) };
    }

    public async Task<ApiResponse> DeleteAsync(string userId, int projectId)
    {
        if (projectId <= 0)
            return new ApiResponse { Success = false, Message = "A valid legacy project is required." };

        var project = await _context.LegacyProjects.SingleOrDefaultAsync(project =>
            project.Id == projectId && project.ApplicationUserId == userId);
        if (project is null)
            return new ApiResponse { Success = false, Message = "Legacy project was not found." };

        _context.LegacyProjects.Remove(project);
        await _context.SaveChangesAsync();
        return new ApiResponse { Success = true, Message = "Legacy project deleted successfully." };
    }

    private static string? NormalizeProvider(string provider) =>
        Providers.SingleOrDefault(item => item.Equals(provider, StringComparison.OrdinalIgnoreCase));

    private static LegacyProjectDto ToDto(LegacyProject project) => new()
    {
        Id = project.Id,
        Provider = project.Provider,
        Name = project.Name,
        Color = project.Color,
        Description = project.Description,
        CreatedAtUtc = project.CreatedAtUtc,
        TotalTimeSeconds = project.TotalTimeSeconds
    };

    private static ApiResponse<T> Failure<T>(string message) => new() { Success = false, Message = message };
}
