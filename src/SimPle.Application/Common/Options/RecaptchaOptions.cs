namespace SimPle.Application.Common.Options;

public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    public string SecretKey { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";

    // When set, any request that submits this exact token bypasses Google verification.
    // Set only in development/test environments. Never set in production.
    public string? DevBypassToken { get; set; }
}
