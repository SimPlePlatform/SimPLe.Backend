using FluentValidation;
using SimPle.Application.Auth.DTOs;

namespace SimPle.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    // Static block-list for the most common weak passwords.
    // A HaveIBeenPwned API check would cover more cases but adds a network call to registration.
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "12345678", "password1", "qwerty123", "iloveyou",
        "admin123", "welcome1", "monkey123", "dragon123", "master123",
        "letmein1", "sunshine", "princess", "baseball", "football",
        "superman", "batman123", "spider123", "pass1234", "test1234",
    };

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username cannot exceed 30 characters.")
            .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(254).WithMessage("Email is too long.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Must(HasSufficientVariety).WithMessage("Password must include at least 2 of: uppercase letters, lowercase letters, numbers, or special characters.")
            .Must(p => !CommonPasswords.Contains(p)).WithMessage("This password is too common. Please choose a stronger password.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Please complete the CAPTCHA check.");
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
