using SimPle.Domain.Common;

namespace SimPle.Domain.Profiles;

public class ProfileExternalLink : Entity
{
    public Guid UserId { get; private set; }
    public string Platform { get; private set; } = default!;  // e.g. "github", "twitter", "instagram"
    public string Url { get; private set; } = default!;
    public string? DisplayLabel { get; private set; }
    public int SortOrder { get; private set; }

    private ProfileExternalLink() { }

    public static ProfileExternalLink Create(Guid userId, string platform, string url, string? displayLabel = null, int sortOrder = 0)
    {
        return new ProfileExternalLink
        {
            UserId = userId,
            Platform = NormalizePlatform(platform),
            Url = NormalizeUrl(url),
            DisplayLabel = displayLabel?.Trim(),
            SortOrder = sortOrder,
        };
    }

    public void Update(string url, string? displayLabel, int sortOrder)
    {
        Url = url.Trim();
        DisplayLabel = displayLabel?.Trim();
        SortOrder = sortOrder;
        Touch();
    }

    private static string NormalizePlatform(string platform)
    {
        var normalized = platform.Trim().ToLowerInvariant();
        return normalized == "twitter" ? "xtwitter" : normalized;
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return trimmed;

        var builder = new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = uri.Host.ToLowerInvariant()
        };
        if (builder.Uri.IsDefaultPort) builder.Port = -1;
        return builder.Uri.ToString().TrimEnd('/');
    }
}
