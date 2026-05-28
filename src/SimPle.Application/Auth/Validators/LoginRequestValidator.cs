using FluentValidation;
using SimPle.Application.Auth.DTOs;

namespace SimPle.Application.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required.")
            .MaximumLength(254).WithMessage("Input is too long.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password is too long.");

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Please complete the CAPTCHA check.");
    }
}
