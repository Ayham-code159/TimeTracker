using FluentValidation;
using TimeTracker.Models.Dtos.ProjectDtos;

namespace TimeTracker.Validators.ProjectValidators;

public class ProjectIdDtoValidator : AbstractValidator<ProjectIdDto>
{
    public ProjectIdDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
