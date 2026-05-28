namespace SimPle.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = default!;
    public string Issuer { get; set; } = "SimPle";
    public string Audience { get; set; } = "SimPle";
    public int ExpiryMinutes { get; set; } = 15;
}
