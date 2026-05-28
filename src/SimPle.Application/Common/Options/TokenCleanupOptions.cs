namespace SimPle.Application.Common.Options;

public sealed class TokenCleanupOptions
{
    public const string SectionName = "TokenCleanup";

    // How often the cleanup job runs.
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    // Delete tokens that expired more than this long ago (gives a small forensic window).
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(1);
}
