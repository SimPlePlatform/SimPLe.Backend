using FluentValidation;
using SimPle.Application.Profiles.DTOs;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateInterestsRequestValidator : AbstractValidator<UpdateInterestsRequestDto>
{
    private static readonly string[] AllowedInterests =
    [
        "board-games", "word-games", "puzzle-games", "strategy-games",
        "arcade-games", "casual-games", "card-games", "trivia"
    ];

    public UpdateInterestsRequestValidator()
    {
        RuleFor(x => x.Interests)
            .NotNull()
            .Must(i => i.Count <= 6).WithMessage("Maximum 6 interests allowed.");

        RuleForEach(x => x.Interests)
            .NotEmpty()
            .MaximumLength(50)
            .Must(i => AllowedInterests.Contains(i.ToLowerInvariant()))
            .WithMessage($"Interest must be one of: {string.Join(", ", AllowedInterests)}.");
    }
}
