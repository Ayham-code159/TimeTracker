using FluentValidation;
using TimeTracker.Models.Dtos.LegacyProjectDtos;

namespace TimeTracker.Validators.LegacyProjectValidators;

public class UpdateLegacyProjectDtoValidator : AbstractValidator<UpdateLegacyProjectDto>
{
    public UpdateLegacyProjectDtoValidator()
    {
        Include(new CreateLegacyProjectDtoValidator());
        RuleFor(project => project.Id).GreaterThan(0);
    }
}
