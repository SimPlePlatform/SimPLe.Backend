using FluentValidation;
using SimPle.Application.Auth.DTOs;

namespace SimPle.Application.Auth.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Must(p => p.Any(char.IsUpper))
            .WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => p.Any(char.IsDigit))
            .WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from the current password.");
    }
}
