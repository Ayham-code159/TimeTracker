using FluentValidation;
using TimeTracker.Models.Dtos.ProjectDtos;

namespace TimeTracker.Validators.ProjectValidators;

public class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => name == name.Trim())
            .WithMessage("Project name must not start or end with spaces.");

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a hex value such as #2563EB.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
