using FluentValidation;
using TimeTracker.Models.Dtos.AuthDtos;

namespace TimeTracker.Validators.AuthValidators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
