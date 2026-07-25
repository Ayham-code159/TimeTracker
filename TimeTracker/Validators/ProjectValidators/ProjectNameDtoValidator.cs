using FluentValidation;
using TimeTracker.Models.Dtos.ProjectDtos;

namespace TimeTracker.Validators.ProjectValidators;

public class ProjectNameDtoValidator : AbstractValidator<ProjectNameDto>
{
    public ProjectNameDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
