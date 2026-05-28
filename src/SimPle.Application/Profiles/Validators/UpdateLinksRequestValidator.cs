using FluentValidation;
using SimPle.Application.Profiles.DTOs;

namespace SimPle.Application.Profiles.Validators;

public sealed class UpdateLinksRequestValidator : AbstractValidator<UpdateLinksRequestDto>
{
    private static readonly string[] AllowedPlatforms =
        ["github", "xtwitter", "twitter", "instagram", "discord", "website"];

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
                .Must(IsHttpsUrl)
                .WithMessage("URL must be a valid absolute HTTPS URL.");

            link.RuleFor(l => l.DisplayLabel)
                .MaximumLength(64)
                .When(l => l.DisplayLabel is not null);
        });
    }

    private static bool IsHttpsUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool HaveNoDuplicatePlatformUrls(IReadOnlyList<LinkItemDto>? links)
    {
        if (links is null) return true;

        return links
            .Select(l => $"{NormalizePlatform(l.Platform)}|{NormalizeUrl(l.Url)}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == links.Count;
    }

    private static string NormalizePlatform(string platform) =>
        string.Equals(platform, "twitter", StringComparison.OrdinalIgnoreCase)
            ? "xtwitter"
            : platform.Trim().ToLowerInvariant();

    private static string NormalizeUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + uri.Query
            : url.Trim();
}
