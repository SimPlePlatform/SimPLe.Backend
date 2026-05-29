using FluentValidation;
using SimPle.Application.Profiles.DTOs;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    private static readonly string[] AllowedVisibilities = ["Public", "FriendsOnly", "Private"];
    private static readonly string[] AllowedProfileTypes = ["Player", "Developer"];

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Bio)
            .MaximumLength(400)
            .When(x => x.Bio is not null);

        RuleFor(x => x.Region)
            .MaximumLength(64)
            .When(x => x.Region is not null);

        RuleFor(x => x.StatusMessage)
            .MaximumLength(100)
            .When(x => x.StatusMessage is not null);

        RuleFor(x => x.Visibility)
            .Must(v => v is null || AllowedVisibilities.Contains(v))
            .WithMessage($"Visibility must be one of: {string.Join(", ", AllowedVisibilities)}.");

        RuleFor(x => x.ProfileType)
            .Must(v => v is null || AllowedProfileTypes.Contains(v))
            .WithMessage($"Profile type must be one of: {string.Join(", ", AllowedProfileTypes)}.");
    }
}
