using FluentValidation;
using SimPle.Application.Profiles.DTOs;
using SimPle.Domain.Profiles;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateLinksRequestValidator : AbstractValidator<UpdateLinksRequestDto>
{
    private static readonly string[] AllowedPlatforms =
        ["github", "xtwitter", "twitter", "instagram", "discord"];

    public UpdateLinksRequestValidator()
    {
        RuleFor(x => x.Links)
            .NotNull()
            .Must(l => l.Count <= 5).WithMessage("Maximum 5 links allowed.")
            .Must(HaveNoDuplicatePlatformUrls).WithMessage("Duplicate platform and URL links are not allowed.");

        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Platform)
                .NotEmpty()
                .Must(p => AllowedPlatforms.Contains(p.ToLowerInvariant()))
                .WithMessage($"Platform must be one of: {string.Join(", ", AllowedPlatforms)}.");

            link.RuleFor(l => l.Url)
                .NotEmpty()
                .MaximumLength(512)
                .Must((item, url) => CanNormalizeForPlatform(item.Platform, url))
                .WithMessage("The URL or handle is not valid for the selected platform.");

            link.RuleFor(l => l.DisplayLabel)
                .MaximumLength(64)
                .When(l => l.DisplayLabel is not null);
        });
    }

    private static bool CanNormalizeForPlatform(string platform, string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var normalizedPlatform = NormalizePlatform(platform);
        try
        {
            ProfileExternalLink.NormalizeUrlForPlatform(normalizedPlatform, url);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HaveNoDuplicatePlatformUrls(IReadOnlyList<LinkItemDto>? links)
    {
        if (links is null) return true;

        return links
            .Select(l =>
            {
                var platform = NormalizePlatform(l.Platform);
                string normalized;
                try { normalized = ProfileExternalLink.NormalizeUrlForPlatform(platform, l.Url); }
                catch { normalized = l.Url.Trim().ToLowerInvariant(); }
                return $"{platform}|{normalized.ToLowerInvariant()}";
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == links.Count;
    }

    private static string NormalizePlatform(string platform) =>
        string.Equals(platform, "twitter", StringComparison.OrdinalIgnoreCase)
            ? "xtwitter"
            : platform.Trim().ToLowerInvariant();
}
