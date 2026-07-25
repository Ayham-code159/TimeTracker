using FluentValidation;
using TimeTracker.Models.Dtos.ProjectDtos;

namespace TimeTracker.Validators.ProjectValidators;

public class AddManualTimeDtoValidator : AbstractValidator<AddManualTimeDto>
{
    public AddManualTimeDtoValidator()
    {
        RuleFor(request => request.Hours)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.Minutes)
            .InclusiveBetween(0, 59);

        RuleFor(request => request)
            .Must(request => request.Hours > 0 || request.Minutes > 0)
            .WithMessage("Manual time must be greater than 00:00.");
    }
}
