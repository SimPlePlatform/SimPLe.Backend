namespace SimPle.Application.Common.Options;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>OAuth 2.0 Client ID from Google Cloud Console. Used to validate the <c>aud</c> claim on incoming ID tokens.</summary>
    public string ClientId { get; init; } = string.Empty;
}
