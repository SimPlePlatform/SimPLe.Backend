using System.Text.RegularExpressions;
using SimPle.Domain.Common;

namespace SimPle.Domain.Profiles;

public class ProfileExternalLink : Entity
{
    public Guid UserId { get; private set; }
    public string Platform { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string? DisplayLabel { get; private set; }
    public int SortOrder { get; private set; }

    private ProfileExternalLink() { }

    public static ProfileExternalLink Create(Guid userId, string platform, string url, string? displayLabel = null, int sortOrder = 0)
    {
        var normalized = NormalizePlatform(platform);
        return new ProfileExternalLink
        {
            UserId = userId,
            Platform = normalized,
            Url = NormalizeUrlForPlatform(normalized, url),
            DisplayLabel = displayLabel?.Trim(),
            SortOrder = sortOrder,
        };
    }

    public void Update(string platform, string url, string? displayLabel, int sortOrder)
    {
        var normalized = NormalizePlatform(platform);
        Platform = normalized;
        Url = NormalizeUrlForPlatform(normalized, url);
        DisplayLabel = displayLabel?.Trim();
        SortOrder = sortOrder;
        Touch();
    }

    private static string NormalizePlatform(string platform)
    {
        var p = platform.Trim().ToLowerInvariant();
        return p == "twitter" ? "xtwitter" : p;
    }

    // Accepts either a bare handle/username or a full profile URL.
    // Returns the canonical profile URL for the platform.
    // Throws ArgumentException if the input cannot be mapped to a valid profile URL.
    public static string NormalizeUrlForPlatform(string platform, string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Link value cannot be empty.");

        return platform switch
        {
            "instagram" => NormalizeHandle(trimmed, "https://www.instagram.com/", new[] { "instagram.com", "www.instagram.com" }),
            "xtwitter"  => NormalizeHandle(trimmed, "https://x.com/", new[] { "x.com", "twitter.com", "www.x.com", "www.twitter.com" }),
            "github"    => NormalizeHandle(trimmed, "https://github.com/", new[] { "github.com", "www.github.com" }),
            "discord"   => NormalizeDiscord(trimmed),
            _           => throw new ArgumentException($"Platform '{platform}' is not supported."),
        };
    }

    // Shared logic: accepts either a handle or a URL whose host is in allowedHosts.
    private static string NormalizeHandle(string input, string baseUrl, string[] allowedHosts)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();
            if (!allowedHosts.Contains(host))
                throw new ArgumentException($"URL host '{host}' is not allowed for this platform.");

            if (uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Only HTTPS links are accepted.");

            var path = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrEmpty(path) || path.Contains('/'))
                throw new ArgumentException("URL must point to a user profile, not a sub-page.");

            var handle = path.TrimStart('@');
            if (!IsValidHandle(handle))
                throw new ArgumentException("The handle contains invalid characters.");

            return baseUrl + handle;
        }

        // Bare handle input.
        var bare = input.TrimStart('@');
        if (!IsValidHandle(bare))
            throw new ArgumentException("Handle contains invalid characters.");

        return baseUrl + bare;
    }

    // Discord: accept a username (stored as discord://{username}) or a discord.gg invite URL.
    private static string NormalizeDiscord(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Only HTTPS Discord links are accepted.");

            var host = uri.Host.ToLowerInvariant();
            if (host != "discord.gg" && host != "discord.com" && host != "www.discord.com")
                throw new ArgumentException("Discord links must use discord.gg or discord.com.");

            return $"https://{host}{uri.AbsolutePath.TrimEnd('/')}";
        }

        // Bare Discord username.
        var username = input.TrimStart('@');
        if (string.IsNullOrEmpty(username) || username.Length > 32)
            throw new ArgumentException("Discord username is invalid.");

        return $"discord://{username}";
    }

    private static bool IsValidHandle(string handle) =>
        !string.IsNullOrEmpty(handle) &&
        handle.Length <= 64 &&
        Regex.IsMatch(handle, @"^[A-Za-z0-9_.\-]+$");
}
