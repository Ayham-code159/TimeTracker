using FluentValidation;
using TimeTracker.Models.Dtos.LegacyProjectDtos;

namespace TimeTracker.Validators.LegacyProjectValidators;

public class CreateLegacyProjectDtoValidator : AbstractValidator<CreateLegacyProjectDto>
{
    public CreateLegacyProjectDtoValidator()
    {
        RuleFor(project => project.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => name == name.Trim())
            .WithMessage("Legacy project name must not start or end with spaces.");

        RuleFor(project => project.Color)
            .NotEmpty()
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a hex value such as #2563EB.");

        RuleFor(project => project.Description).MaximumLength(1000);
    }
}
