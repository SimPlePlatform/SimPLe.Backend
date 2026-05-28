using FluentValidation;
using SimPle.Application.Profiles.DTOs;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateLinksRequestValidator : AbstractValidator<UpdateLinksRequestDto>
{
    private static readonly string[] AllowedPlatforms =
        ["github", "twitter", "instagram", "discord", "website", "youtube", "twitch", "linkedin"];

    public UpdateLinksRequestValidator()
    {
        RuleFor(x => x.Links)
            .NotNull()
            .Must(l => l.Count <= 8).WithMessage("Maximum 8 links allowed.");

        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Platform)
                .NotEmpty()
                .Must(p => AllowedPlatforms.Contains(p.ToLowerInvariant()))
                .WithMessage($"Platform must be one of: {string.Join(", ", AllowedPlatforms)}.");

            link.RuleFor(l => l.Url)
                .NotEmpty()
                .MaximumLength(512)
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                .WithMessage("URL must be a valid absolute URL.");

            link.RuleFor(l => l.DisplayLabel)
                .MaximumLength(64)
                .When(l => l.DisplayLabel is not null);
        });
    }
}
