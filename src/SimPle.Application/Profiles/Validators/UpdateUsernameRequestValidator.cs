using FluentValidation;
using SimPle.Application.Profiles.DTOs;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateUsernameRequestValidator : AbstractValidator<UpdateUsernameRequestDto>
{
    public UpdateUsernameRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30)
            .Matches(@"^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may only contain letters, digits, underscores, dots, and hyphens.");
    }
}
