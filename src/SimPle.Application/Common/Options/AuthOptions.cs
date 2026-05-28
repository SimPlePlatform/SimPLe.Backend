namespace SimPle.Application.Common.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int RefreshTokenExpiryDays { get; set; } = 7;
    public int MaxFailedLoginAttempts { get; set; } = 10;
    public int LockoutDurationMinutes { get; set; } = 15;
}
