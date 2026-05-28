using FluentValidation;
using SimPle.Application.Auth.DTOs;

namespace SimPle.Application.Auth.Validators;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Must(HasSufficientVariety).WithMessage("Password must include at least 2 of: uppercase letters, lowercase letters, numbers, or special characters.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }

    private static bool HasSufficientVariety(string password)
    {
        var categories = 0;
        if (password.Any(char.IsUpper)) categories++;
        if (password.Any(char.IsLower)) categories++;
        if (password.Any(char.IsDigit)) categories++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) categories++;
        return categories >= 2;
    }
}
